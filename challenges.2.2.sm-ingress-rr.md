# Configure Ingress Routes and Request Routing #

## Install Destionation Rules ##

```yaml
apiVersion: networking.istio.io/v1alpha3
kind: DestinationRule
metadata:
  name: calcfrontend-rule
  namespace: challenge2
spec:
  host: calcfrontendsvc
  subsets:
  - name: v2
    labels:
      version: v2
---
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

Check Destination Rules

```shell
$ kubectl get destinationrules -n challenge2

NAME                AGE
calcbackend-rule    1m
calcfrontend-rule   1m
```

Install Gateway and Virtaul Services

```yaml
apiVersion: networking.istio.io/v1alpha3
kind: Gateway
metadata:
  name: frontend-gateway
  namespace: challenge2 
spec:
  selector:
    istio: ingressgateway
  servers:
  - port:
      number: 80
      name: http
      protocol: HTTP
    hosts:
    - "*"
---
apiVersion: networking.istio.io/v1alpha3
kind: VirtualService
metadata:
  name: app-vs
  namespace: challenge2  
spec:
  hosts:
  - "*"
  gateways:
  - frontend-gateway
  http:
  - match:
    - uri:
        exact: /api/calculation
    - uri:
        prefix: /
    route:
    - destination:
        host: calcfrontendsvc
        port:
          number: 80
---
apiVersion: networking.istio.io/v1alpha3
kind: VirtualService
metadata:
  name: frontend-vs
  namespace: challenge2
spec:
  hosts:
  - calcfrontendsvc
  http:
    - route:
      - destination:
          host: calcfrontendsvc
          subset: v2
---
apiVersion: networking.istio.io/v1alpha3
kind: VirtualService
metadata:
  name: backend-vs
  namespace: challenge2
spec:
  hosts:
  - calcbackendsvc
  http:
    - route:
      - destination:
          host: calcbackendsvc
          subset: v1
```

Check Routing Rules and Gateway Installation

```shell
$ kubectl apply -f .\c2-ingress-rr.yaml

gateway.networking.istio.io "frontend-gateway" created
virtualservice.networking.istio.io "app-vs" created
virtualservice.networking.istio.io "frontend-vs" created
virtualservice.networking.istio.io "backend-vs" created
```

```shell
$ kubectl get virtualservices -n challenge2

NAME          AGE
app-vs        14s
backend-vs    13s
frontend-vs   14s

$ kubectl get gateways -n challenge2

NAME               AGE
frontend-gateway   1m
```

## Weigth-based Routing ##

Apply c2-netcorcalcbackend-v2 (Version 2 netcalccore)

Apply c2-ingress-rr-v2-50weight (50:50 weight based routing)

## User-Agent Routing ##

Apply c2-ingress-rr-v2-mobile (Mobile Users use V2, others v1)