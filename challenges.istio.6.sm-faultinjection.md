# Implement Fault Injection #

**Fault Injection** is a technique to test the robustness of your application by deliberately introducing faults. In particular, it is often used to test the error handling code paths of your services. Especially in the microservice space it is important to handle failures in service-to-service calls gracefully, because there are "a lot of moving parts" where failure can occur unexpectedly while communication happens.

## Here is what you will learn ##

- get to know the types of possible failure injection with Istio
- add failures to the backend service

## Reset application ##

First, let's reset the application to an intial state and test the environment.

Apply destination rule and deployment:

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
      weight: 50
    - destination:
        host: calcbackendsvc
        subset: v2
      weight: 50
```

Now open up a browser window and check that everything works as expected.

## Inject Faults ##


