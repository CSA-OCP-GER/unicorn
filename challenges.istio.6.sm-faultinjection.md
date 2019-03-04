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

Configure the VirtualService:

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

Now, it's time to add some faults during communication inside our service mesh, to test the resiliancy of our application.

### Add failure for v1 and v2 ###

With the following `VirtualService` definition, we add 30% failure to our `v1` and `v2` services (Http StatusCode **500**).

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
    fault:
      abort:
        percent: 30
        httpStatus: 500
```

Open the browser an see errors appear.

### Simulate high latency ###

Now, let's add another common scenario: high service latency. Any cloud native application also has to deal with services, that may experience high response times now and then. Your application should be able to deal with such situations.

Simulate "high latency" with the following definition:

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
    fault:
      delay:
        percent: 30
        fixedDelay: 3s
```

Open the browser an see some service calls being delayed for approximately 3 seconds.

## How to respond to such scenarios / Best practices ##

Of course, you have to handle these kinds of failures/scenarios in your source code. Developers should always implement proper error handling and use timeouts/retry mechanisms when calling internal/external services.

There are a lot of libraries in different languages, that can help you with that. E.g. if you are using .NET there is a NuGet package called **Polly** (https://github.com/App-vNext/Polly), for NodeJS you can use http clients like **Axios** (https://github.com/axios/axios)/ **Axios-Retry** (https://github.com/softonic/axios-retry) or **Request** (https://github.com/request/request) that have mechanisms for dealing with timeouts and errors. Go has also a very popular library called **Go-Resiliency** (https://github.com/eapache/go-resiliency) etc.

## House-Keeping ##

Reset the VirtualService definition to baseline:

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