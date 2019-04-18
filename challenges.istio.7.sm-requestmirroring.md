# New Service Versions with Request Mirroring #

Traffic mirroring, also called shadowing, is a powerful concept that allows feature teams to bring changes to production with as little risk as possible. Mirroring sends a copy of live traffic to a mirrored service. With this technique, it is possible for developers to test new service versions with production traffic and be sure that responses of mirrored request will never hit the user.

## Here is what you will learn ##

- Learn how to mirror requests of services running in Istio
- Learn how to compare service versions with Grafana

## Setup ## 

First, let's install the base destination rule and virtual services.

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

## Add a new version ##

Now, add a new version of our service. Your developers came up with a new algorithm for prime factor calculation and think the application would benefit from it. Let's see by mirroring requests of our "old" service to the new version.

### Deploy V3 version jscalbackend-new:2.0 ###

```yaml
apiVersion: extensions/v1beta1
kind: Deployment
metadata:
  name: jscalcbackend-new-v2
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
        name: jscalcbackend-new
        app: backend
        version: v3
    spec:
      containers:
      - name: jscalcbackend-new
        image: csaocpger/jscalcbackend-new:2.0
        ports:
          - containerPort: 80
            name: http
            protocol: TCP
        env: 
          - name: "PORT"
            value: "80"
```

### Add destination rule for v3 ###

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
  - name: v3
    labels:
      version: v3
```

### Adjust the virtual service for mirroring ###

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
    mirror:
      host: calcbackendsvc
      subset: v3
```

The important part is the `mirror` section in the `VirtualService` definition. In this section, we tell the Istio proxy to send requests that have the destination `calcbackendsvc` with subset `v1` also to subset `v3` - but without (!) returning the response. By using this technique, you can test a new version of your service in production and be sure, that possible errors or performance issues will never hit your end-users.

## Open the Grafana Dashboard and compare v1 against v3 ##

Now, it's time to check whether our developers were right and that the new service performs better (in terms of calculation time/mermory consumption etc.) than our "old" service. Run the application in "loop-mode" for some time...then...

Go to the Grafana Dashboard "Istio Service Mesh" and select the services you wish to compare under "Service Workload".

> In a newer version of Grafana, the Dashboard is called "Istio Service Dashboard" and the chart is named "Incoming Request Duration by Source"

How would you decide?

> Also check in the browser, that no requests are served to the frontend with service `v3`!

![Istio Service Mesh](/img/grafana_compare.png)

## House-Keeping ##

Reset your service mesh to "baseline":

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
