# External Service Calls #

Deploy baseline workload

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
      weight: 100
```

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
```

Check, if the application is running as expected.

Now, deploy the new service (calling https://httpbin.org/post)

```yaml
apiVersion: extensions/v1beta1
kind: Deployment
metadata:
  name: jscalcbackend-egress-v1
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

add destination rule and ingress/vs

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
  - name: v4
    labels:
      version: v4
```

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
        subset: v4
      weight: 50
```

Check the website and call a few times the calculation service. See how errors appear stating that the external call falied.

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

See how the service calls are now working.