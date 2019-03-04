# Implement the Circuit Breaker Pattern #

In microservice architectures, service calls - due to their nature being remote calls - sometimes fail. They fail, because a service is unavailable or suffers from high traffic and is unresponsible/has very high latency. Precious resources such as threads might be consumed in the caller while waiting for the other service to respond. This might lead to resource exhaustion, which would make the calling service unable to handle other requests. The failure of one service can potentially cascade to other services throughout the whole application. 

The solution to such scenarios is the **Circuit Breaker** pattern. When the number of consecutive failures crosses a threshold, the circuit breaker trips, and for the duration of a timeout period all attempts to invoke the remote service will fail immediately. After the timeout expires the circuit breaker allows a limited number of test requests to pass through. If those requests succeed the circuit breaker resumes normal operation. Otherwise, if there is a failure the timeout period begins again.

![Circuit Breaker](/img/circuitbreaker.png)

## Here is what you will learn ##

- implement the Circuit Breaker pattern with Istio by simulating a service with high latency
- circuit tripping based on pending requests

## Deploy Service with a timeout / destination rules / routing rules ##

First, we need to deploy a service that simulates the "high latency" problem.

```yaml
apiVersion: extensions/v1beta1
kind: Deployment
metadata:
  name: jscalcfrontend-v2
  namespace: challengeistio
spec:
  replicas: 1
  minReadySeconds: 5
  strategy:
    type: RollingUpdate
    rollingUpdate:
      maxSurge: 1
      maxUnavailable: 1
  template:
    metadata:
      labels:
        name: jscalcfrontend
        app: frontend
        version: v2
    spec:
      containers:
      - name: jscalcfrontend
        image: csaocpger/jscalcfrontend:9.0
        ports:
          - containerPort: 80
            name: http         
            protocol: TCP
        env: 
          - name: "ENDPOINT"
            value: "calcbackendsvc"
          - name: "PORT"
            value: "80"
---
apiVersion: extensions/v1beta1
kind: Deployment
metadata:
  name: jscalctimeout-v3-timeout
  namespace: challengeistio
spec:
  replicas: 1
  minReadySeconds: 5
  strategy:
    type: RollingUpdate
    rollingUpdate:
      maxSurge: 1
      maxUnavailable: 1
  template:
    metadata:
      labels:
        name: jscalctimeout
        app: backend
        version: v3-timeout
    spec:
      containers:
      - name: jscalctimeout
        image: csaocpger/jscalctimeout:1.0
        ports:
          - containerPort: 80
            name: http
            protocol: TCP
        env: 
          - name: "PORT"
            value: "80"
```

Next, deploy the destination rules.

```yaml
apiVersion: networking.istio.io/v1alpha3
kind: DestinationRule
metadata:
  name: calcbackend-rule
  namespace: challengeistio
spec:
  host: calcbackendsvc
  subsets:
  - name: v1
    labels:
      version: v1
  - name: v2
    labels:
      version: v2
  - name: v3-timeout
    labels:
      version: v3-timeout
```

...finally, the routing rules.

```yaml
apiVersion: networking.istio.io/v1alpha3
kind: VirtualService
metadata:
  name: backend-vs
  namespace: challengeistio
spec:
  hosts:
  - calcbackendsvc
  http:
  - match:
    - headers:
        user-agent:
          regex: .*Mobile.*
    route:
      - destination:
          host: calcbackendsvc
          subset: v2
  - route:
    - destination:
        host: calcbackendsvc
        subset: v1
      weight: 50
    - destination:
        host: calcbackendsvc
        subset: v3-timeout
      weight: 50
```

### Check the application ###

Open a browser window and start the number calculation (loop). You can see, that some requests are really slow (~2sec) - the ones that are served by the new "timeout-service". 

In a "highly frequented service" scenario, this particular service can be a problem for the whole application.


## Deploy Circuit Breaker rules ##

We are now going to implement the "Circuit Breaker" pattern, to immediatly send an error to the caller when more than one http request is in pending state.

```yaml
apiVersion: networking.istio.io/v1alpha3
kind: DestinationRule
metadata:
  name: calcbackend-rule
  namespace: challengeistio
spec:
  host: calcbackendsvc
  subsets:
  - name: v1
    labels:
      version: v1
  - name: v2
    labels:
      version: v2
  - name: v3-timeout
    labels:
      version: v3-timeout
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

Now check the application again and see how errors are immediately thrown when running the number calculation in a loop (and it's hitting the "timeout service" twice in a short period of time).