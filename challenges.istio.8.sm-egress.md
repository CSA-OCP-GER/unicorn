# External Service Calls #

By default, Istio-enabled services are unable to access URLs outside of your cluster, because the pod uses iptables to transparently redirect all outbound traffic to the sidecar proxy, which only handles intra-cluster destinations.

> NOTE: This behavior has changed in Istio 1.1! The default outbound traffic mode is `ALLOW_ANY`. All extrenal calls will succeed by default. Anyway, you still can control whether traffic is allowed to leave the service mesh or not. If you are using Istio 1.1.x please first changed the default behavior, see section **Isitio 1.1 Pre-Requisites**

This is why external calls will fail by default. You have to explicitly enable them by "whitlisting" these services via `ServiceEntry` definitions.

## Here is what you will learn ##

- Learn how create `ServiceEntry` definitions
- Enable Istio services to call external services

## Istio 1.1 Pre-Requisites ##

In Istio 1.1, you have to enable egress-traffic control via the Istio ConfigMap.

Edit the configmap by executing `kubectl edit configmap istio -n istio-system` and replacing `ALLOW_ANY` with `REGISTRY_ONLY`.

> There should be two occurences of the term ÀLLOW_ANY`. Now, wait several seconds before all proxys are updated with the new configuration.

## Setup ##

Deploy baseline workload (VirtaulService and DestinationRule):

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
      weight: 100
```

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
```

Check in your browser, if the application is running as expected.

## Test a service that wants to make a request outside of your service mesh ##

Deploy the new service *jscalcbackend-egress*. That service will be making a service call to https://httpbin.org/post, see:

```js
axios.post('https://httpbin.org/post', {}, {
    headers: {
        accept: 'application/json'
    }
}).then(() => {
    return res.send(serverResult.toString());
}).catch(() => {
    return res.send(500, "Failed to call httpbin resource. Adjust egress rules!");
});
```

Deploy pods:

```yaml
apiVersion: extensions/v1beta1
kind: Deployment
metadata:
  name: jscalcbackend-egress-v1
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
        name: jscalcbackend-egress
        app: backend
        version: v4
    spec:
      containers:
      - name: jscalcbackend-egress
        image: csaocpger/jscalcbackend-egress:1.0
        ports:
          - containerPort: 80
            name: http
            protocol: TCP
        env: 
          - name: "PORT"
            value: "80"
```

Add destination rule and VirtualService definition:

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
  - name: v4
    labels:
      version: v4
```

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
        subset: v4
      weight: 50
```

Check the website and call the calculation service a few times. See how errors appear stating that the external service call failed.

## Add ServiceEntry ##

Now, let's add the egress definition. Please be aware that, if the external service must be reached via TLS, you also need to define a VirtualService.

```yaml
apiVersion: networking.istio.io/v1alpha3
kind: ServiceEntry
metadata:
  name: httpbin-ext
spec:
  hosts:
  - httpbin.org
  ports:
  - number: 443
    name: https
    protocol: HTTPS
  resolution: DNS
  location: MESH_EXTERNAL
---
apiVersion: networking.istio.io/v1alpha3
kind: VirtualService
metadata:
  name: httpbin-ext
spec:
  hosts:
  - httpbin.org
  tls:
  - match:
    - port: 443
      sni_hosts:
      - httpbin.org
    route:
    - destination:
        host: httpbin.org
        port:
          number: 443
      weight: 100
```

Check your browser again and see how the service calls are now working.
