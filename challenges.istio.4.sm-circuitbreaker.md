# Implement the Circuit Breaker Pattern #

In microservice architectures, service calls - due to their nature being remote calls - sometimes fail. They fail, because a service is unavailable or suffers from high traffic and is unresponsible/has very high latency. Precious resources such as threads might be consumed in the caller while waiting for the other service to respond. This might lead to resource exhaustion, which would make the calling service unable to handle other requests. The failure of one service can potentially cascade to other services throughout the whole application. 

The solution to such scenarios is the **Circuit Breaker** pattern. When the number of consecutive failures crosses a threshold, the circuit breaker trips, and for the duration of a timeout period all attempts to invoke the remote service will fail immediately. After the timeout expires the circuit breaker allows a limited number of test requests to pass through. If those requests succeed the circuit breaker resumes normal operation. Otherwise, if there is a failure the timeout period begins again.

![Circuit Breaker](/img/circuitbreaker.png)

## Here is what you will learn ##

- implement the Circuit Breaker pattern with Istio by simulating a service with high latency
- circuit tripping based on pending requests

## Deploy Service with a timeout / destination rules / routing rules ##

First, we need to deploy a service that serves as our backend. We therefore deploy a pod called `httpbin`. The pod just does the same thing as the httpbin service - just in a container.

```yaml
apiVersion: v1
kind: Service
metadata:
  name: httpbin
  namespace: challengeistio
  labels:
    app: httpbin
spec:
  ports:
  - name: http
    port: 8000
  selector:
    app: httpbin
---
apiVersion: extensions/v1beta1
kind: Deployment
metadata:
  name: httpbin
spec:
  replicas: 1
  template:
    metadata:
      labels:
        app: httpbin
        version: v1
    spec:
      containers:
      - image: docker.io/citizenstig/httpbin
        imagePullPolicy: IfNotPresent
        name: httpbin
        ports:
        - containerPort: 8000
```

Next, deploy the destination rules. to be able to route request to the new service.

```yaml
apiVersion: networking.istio.io/v1alpha3
kind: DestinationRule
metadata:
  name: httpbin
  namespace: challengeistio
spec:
  host: httpbin
  trafficPolicy:
    connectionPool:
      tcp:
        maxConnections: 1
      http:
        http1MaxPendingRequests: 1
        maxRequestsPerConnection: 1
    outlierDetection:
      consecutiveErrors: 1
      interval: 1s
      baseEjectionTime: 3m
      maxEjectionPercent: 100
```

You can see in the above YAML definition, that we are also setting a `trafficPolicy` that tells Istio/Envoy to only allow 1 pending request and 1 concurrent connection to our service. If there is more than 1 request waiting, these requests will be cancelled with a http status code `503`.

### Check the service ###

First, let's see if the service can be accessed and that it is answering our requests. Therefor, we need a client.

```yaml
apiVersion: apps/v1beta1
kind: Deployment
metadata:
  name: fortio-deploy
  namespace: challengeistio
spec:
  replicas: 1
  template:
    metadata:
      labels:
        app: fortio
    spec:
      containers:
      - name: fortio
        image: istio/fortio:latest
        imagePullPolicy: Always
        ports:
        - containerPort: 8080
          name: http-fortio
        - containerPort: 8079
          name: grpc-ping
```

Check the deployment and note down the name of the `fortio` pod:

```shell
$ kubectl get po -n challengeistio

NAME                                       READY     STATUS    RESTARTS   AGE
fortio-deploy-5b47b8cb97-jg5x6             1/2       Running   0          13m
gocalcbackend-v1-68cb9b4dc6-dqjrt          2/2       Running   0          19h
httpbin-5765746fb8-tzhlq                   2/2       Running   0          13m
jscalcbackend-v1-7bf649d56c-4qtp8          2/2       Running   0          19h
jscalcfrontend-v2-7545f9c99c-f9nqw         2/2       Running   0          18h
jscalctimeout-v3-timeout-f6dbd9cb4-jnkr8   2/2       Running   0          18h
netcorecalcbackend-v1-867cd78fc6-57kcs     2/2       Running   0          19h
netcorecalcbackend-v2-7db75669d-fk2v9      2/2       Running   0          19h
```

Now, let's send a request from our client to the httpbin service to see, if the scenario is working:

```
$ kubectl exec -n challengeistio -it <FORTIO_POD_NAME> -c fortio /usr/bin/fortio -- load -curl  http://httpbin:8000/get

HTTP/1.1 200 OK
server: envoy
date: Thu, 18 Apr 2019 08:45:37 GMT
content-type: application/json
access-control-allow-origin: *
access-control-allow-credentials: true
content-length: 436
x-envoy-upstream-service-time: 5

{
  "args": {},
  "headers": {
    "Content-Length": "0",
    "Host": "httpbin:8000",
    "User-Agent": "fortio.org/fortio-1.3.2-pre",
    "X-B3-Parentspanid": "f630c49683f495eb",
    "X-B3-Sampled": "1",
    "X-B3-Spanid": "47ef9ff4ae22eee5",
    "X-B3-Traceid": "b248972faf3a26bdf630c49683f495eb",
    "X-Request-Id": "415bf0ec-4dc7-9f89-86f3-4d39862a884f"
  },
  "origin": "127.0.0.1",
  "url": "http://httpbin:8000/get"
}
```

You see, we get a response, so our service ready.


## Tripping the Circuit Breaker ##

We are now going to call our service in a way, that the Circuit Breaker will react and cancel reuests that will exceed the limits we set in the `DestinationRule`. 

Call the service with two concurrent connections (`-c 2`) and send 20 requests (`-n 20`):

```shell
$ kubectl exec -n challengeistio -it <FORTIO_POD_NAME> -c fortio /usr/bin/fortio -- load -c 2 -qps 0 -n 20 -loglevel Warning http://httpbin:8000/get

Fortio 1.3.2-pre running at 0 queries per second, 2->2 procs, for 20 calls: http://httpbin:8000/get
Starting at max qps with 2 thread(s) [gomax 2] for exactly 20 calls (10 per thread + 0)
09:06:42 W http_client.go:683> Parsed non ok code 503 (HTTP/1.1 503)
09:06:42 W http_client.go:683> Parsed non ok code 503 (HTTP/1.1 503)
09:06:42 W http_client.go:683> Parsed non ok code 503 (HTTP/1.1 503)
Ended after 75.208108ms : 20 calls. qps=265.93
Aggregated Function Time : count 20 avg 0.0074062316 +/- 0.01156 min 0.00053822 max 0.041993468 sum 0.148124632
# range, mid point, percentile, count
>= 0.00053822 <= 0.001 , 0.00076911 , 10.00, 2
> 0.001 <= 0.002 , 0.0015 , 15.00, 1
> 0.003 <= 0.004 , 0.0035 , 55.00, 8
> 0.004 <= 0.005 , 0.0045 , 85.00, 6
> 0.006 <= 0.007 , 0.0065 , 90.00, 1
> 0.04 <= 0.0419935 , 0.0409967 , 100.00, 2
# target 50% 0.003875
# target 75% 0.00466667
# target 90% 0.007
# target 99% 0.0417941
# target 99.9% 0.0419735
Sockets used: 5 (for perfect keepalive, would be 2)
Code 200 : 17 (85.0 %)
Code 503 : 3 (15.0 %)
Response Header Sizes : count 20 avg 195.6 +/- 82.17 min 0 max 231 sum 3912
Response Body/Total Sizes : count 20 avg 602.35 +/- 151.8 min 241 max 667 sum 12047
All done 20 calls (plus 0 warmup) 7.406 ms avg, 265.9 qps
```

You see, only 85% of the requests were delivered to the service, 15% have been cancelled with status code `503`.

Now, give it some work to do:

```shell
$ kubectl exec -n challengeistio -it <FORTIO_POD_NAME> -c fortio /usr/bin/fortio -- load -c 4 -qps 0 -n 50 -loglevel Warning http://httpbin:8000/get

Fortio 1.3.2-pre running at 0 queries per second, 2->2 procs, for 50 calls: http://httpbin:8000/get
Starting at max qps with 4 thread(s) [gomax 2] for exactly 50 calls (12 per thread + 2)
09:11:07 W http_client.go:683> Parsed non ok code 503 (HTTP/1.1 503)
09:11:07 W http_client.go:683> Parsed non ok code 503 (HTTP/1.1 503)
09:11:07 W http_client.go:683> Parsed non ok code 503 (HTTP/1.1 503)
09:11:07 W http_client.go:683> Parsed non ok code 503 (HTTP/1.1 503)
09:11:07 W http_client.go:683> Parsed non ok code 503 (HTTP/1.1 503)
...
...
09:11:07 W http_client.go:683> Parsed non ok code 503 (HTTP/1.1 503)
09:11:07 W http_client.go:683> Parsed non ok code 503 (HTTP/1.1 503)
Ended after 126.995446ms : 50 calls. qps=393.71
Aggregated Function Time : count 50 avg 0.008991814 +/- 0.0234 min 0.000341213 max 0.090395478 sum 0.449590699
# range, mid point, percentile, count
>= 0.000341213 <= 0.001 , 0.000670606 , 48.00, 24
> 0.001 <= 0.002 , 0.0015 , 64.00, 8
> 0.003 <= 0.004 , 0.0035 , 72.00, 4
> 0.004 <= 0.005 , 0.0045 , 82.00, 5
> 0.005 <= 0.006 , 0.0055 , 84.00, 1
> 0.006 <= 0.007 , 0.0065 , 90.00, 3
> 0.009 <= 0.01 , 0.0095 , 92.00, 1
> 0.08 <= 0.09 , 0.085 , 98.00, 3
> 0.09 <= 0.0903955 , 0.0901977 , 100.00, 1
# target 50% 0.001125
# target 75% 0.0043
# target 90% 0.007
# target 99% 0.0901977
# target 99.9% 0.0903757
Sockets used: 36 (for perfect keepalive, would be 4)
Code 200 : 16 (32.0 %)
Code 503 : 34 (68.0 %)
Response Header Sizes : count 50 avg 73.66 +/- 107.4 min 0 max 231 sum 3683
Response Body/Total Sizes : count 50 avg 377.06 +/- 198.3 min 241 max 667 sum 18853
All done 50 calls (plus 0 warmup) 8.992 ms avg, 393.7 qps
```

You can see, the CircuitBreaker pattern is a very effective technique to limit the amount of traffic to your services and to be able to keep your application responsive although there is a lot of traffic going on in the cluster.