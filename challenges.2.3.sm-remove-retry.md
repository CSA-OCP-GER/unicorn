# Remove Slow/Buggy Containers #

> Need help? All neccessary files are [here :blue_book:](hints/yaml/challenge-2/poolejector)!

## Deploy new JS Backend (with errors) ##

```yaml
apiVersion: extensions/v1beta1
kind: Deployment
metadata:
  name: jscalcerror-v3-error
  namespace: challenge2
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
        name: jscalcerror
        app: backend
        version: v3-error
    spec:
      containers:
      - name: jscalcerror
        image: csaocpger/jscalcerror:6.0
        ports:
          - containerPort: 80
            name: http
            protocol: TCP
        env:
          - name: "PORT"
            value: "80"
```

[Deployment File :blue_book:](hints/yaml/challenge-2/poolejector/c2-jscalcerror-v3-error.yaml)!

## Add Destionation Rules ##

```yaml
apiVersion: networking.istio.io/v1alpha3
kind: DestinationRule
metadata:
  name: calcbackend-rule
  namespace: challenge2
spec:
  host: calcbackendsvc
  subsets:
  - name: v1
    labels:
      version: v1
  - name: v2
    labels:
      version: v2
  - name: v3-error
    labels:
      version: v3-error
```

[Deployment File :blue_book:](hints/yaml/challenge-2/poolejector/c2-destination-rule-error.yaml)!

## Include Error-Backend in Request Routing ##

```yaml
apiVersion: networking.istio.io/v1alpha3
kind: VirtualService
metadata:
  name: backend-vs
  namespace: challenge2
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
        subset: v3-error
      weight: 50
```

[Deployment File :blue_book:](hints/yaml/challenge-2/poolejector/c2-ingress-rr-v3-error.yaml)!

## See how errors are thrown ##

Open browser an run calculation in loop and see how errors appear approximately 2 minutes after the deployment of the JS "error backend".

## Add traffic policy to remove/suspend buggy pods ##

```yaml
apiVersion: networking.istio.io/v1alpha3
kind: DestinationRule
metadata:
  name: calcbackend-rule
  namespace: challenge2
spec:
  host: calcbackendsvc
  subsets:
  - name: v1
    labels:
      version: v1
  - name: v2
    labels:
      version: v2
  - name: v3-error
    labels:
      version: v3-error
    trafficPolicy:
      connectionPool:
        http: {}
        tcp: {}
      outlierDetection:
        baseEjectionTime: 3m
        consecutiveErrors: 1
        interval: 10s
        maxEjectionPercent: 100
```

See how pods are removed after errors appear and be brought back after 3 minutes via `kubectl get po -n challenge2 -w`

## Add retry-strategies ##

```yaml
apiVersion: networking.istio.io/v1alpha3
kind: VirtualService
metadata:
  name: backend-vs
  namespace: challenge2
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
        subset: v3-error
      weight: 50
    retries:
      attempts: 3
      perTryTimeout: 2s
```

See how errors disappear...