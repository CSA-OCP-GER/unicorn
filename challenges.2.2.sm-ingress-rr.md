# Configure Ingress Routes and Request Routing #

## Here is what you learn ##

- Make the sample application available through the Istio ingress controller
  - create destination rules 
  - enable routing based on service subsets / Kubernetes labels 
  - Weight-based routing
  - Routing based on header values

## Install Destionation Rules ##

### What is it for? ###

> A **destination** indicates the network addressable service to which the request/connection will be sent. A **DestinationRule** defines policies that apply to traffic intended for a service after routing has occurred. (*Source: Istio Documentation*)

...and...

> Subsets can be used for scenarios like A/B testing, or routing to a specific version of a service. (*Source: Istio Documentation*)

So, to be able to communicate with our service via the service mesh, we need to create the corresponding desination rules.

Let's create one for the frontend-service and one for the backend-service:

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

## Install Gateway and Virtual Services ##

### What is it for? ###

> A **Gateway** describes a load balancer operating at the edge of the mesh receiving incoming or outgoing HTTP/TCP connections. The specification describes a set of ports that should be exposed, the type of protocol to use, SNI configuration for the load balancer, etc.

> A **VirtualService** defines a set of traffic routing rules to apply when a host is addressed. Each routing rule defines matching criteria for traffic of a specific protocol. If the traffic is matched, then it is sent to a named destination service (or subset/version of it) defined in the registry. The source of traffic can also be matched in a routing rule. This allows routing to be customized for specific client contexts.

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

$ kubectl describe svc/istio-ingressgateway -n istio-system
```
Copy the Load Balancer IP of the Ingress Gateway an open the browser: http://<INGRESS_GATEWAY_IP>/

You should see something like this:

![Result](img/result_ingress_gateway.png)

## Weigth-based Routing ##

Apply c2-netcorcalcbackend-v2 (Version 2 netcalccore)

Apply c2-ingress-rr-v2-50weight (50:50 weight based routing)

## User-Agent Routing ##

Apply c2-ingress-rr-v2-mobile (Mobile Users use V2, others v1)