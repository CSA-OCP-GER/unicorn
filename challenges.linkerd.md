# Linkerd #

## Introduction to Linkerd ##

Linkerd is the second "player" in the service mesh space.

> *more intro....*

## Install Linkerd ##

```shell
$ curl -sL https://run.linkerd.io/install | sh
...
```

```shell
$ linkerd check --pre

kubernetes-api
--------------
√ can initialize the client
√ can query the Kubernetes API

kubernetes-version
------------------
√ is running the minimum Kubernetes API version
√ is running the minimum kubectl version

pre-kubernetes-setup
--------------------
√ control plane namespace does not already exist
√ can create Namespaces
√ can create ClusterRoles
√ can create ClusterRoleBindings
√ can create CustomResourceDefinitions
√ can create PodSecurityPolicies

[...]

Status check results are √
```

Now install:

```shell
$ linkerd install | kubectl apply -f -

namespace/linkerd created
clusterrole.rbac.authorization.k8s.io/linkerd-linkerd-identity created
clusterrolebinding.rbac.authorization.k8s.io/linkerd-linkerd-identity created
serviceaccount/linkerd-identity created
clusterrole.rbac.authorization.k8s.io/linkerd-linkerd-controller created
clusterrolebinding.rbac.authorization.k8s.io/linkerd-linkerd-controller created
serviceaccount/linkerd-controller created
clusterrole.rbac.authorization.k8s.io/linkerd-linkerd-destination created
clusterrolebinding.rbac.authorization.k8s.io/linkerd-linkerd-destination created
serviceaccount/linkerd-destination created
role.rbac.authorization.k8s.io/linkerd-heartbeat created
rolebinding.rbac.authorization.k8s.io/linkerd-heartbeat created
serviceaccount/linkerd-heartbeat created
clusterrolebinding.rbac.authorization.k8s.io/linkerd-linkerd-web-admin created
serviceaccount/linkerd-web created
customresourcedefinition.apiextensions.k8s.io/serviceprofiles.linkerd.io created

[...]

```

Check the installation

```shell
$ linkerd check
kubernetes-api
--------------
√ can initialize the client
√ can query the Kubernetes API

kubernetes-version
------------------
√ is running the minimum Kubernetes API version
√ is running the minimum kubectl version

[...]

Status check results are √
```

## Application Installation ##

Create a namespace

```shell
$ kubectl create ns challengelinkerd

namespace/challengelinkerd created
```

Annotate namespace for automatic sidecar injection

```shell
$ kubectl annotate ns challengelinkerd "linkerd.io/inject"=enabled

namespace/challengelinkerd annotated
```

Add sample application

BACKEND...

```yaml
apiVersion: v1
kind: Service
metadata:
  name: quotes-backend
  namespace: challengelinkerd
  labels:
    name: quotes-backend
    app: backend
spec:
  ports:
  - port: 3000
    name: http
    targetPort: 3000
  selector:
    app: backend
---
apiVersion: extensions/v1beta1
kind: Deployment
metadata:
  name: quotesbackend
  namespace: challengelinkerd
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
        name: quotesbackend
        app: backend
        version: v1
    spec:
      containers:
      - name: calcbackend
        image: csaocpger/quotesbackend:4
        ports:
          - containerPort: 3000
            name: http
            protocol: TCP
        env:
          - name: "FAIL_ENABLED"
            value: "0"
---
apiVersion: v1
kind: Service
metadata:
  name: quotesgatewaysvc
  namespace: challengelinkerd
  labels:
    name: quotesgatewaysvc
    app: gateway
spec:
  type: LoadBalancer
  selector:
    app: gateway
  ports:
   - port: 80
     name: http
     targetPort: 3000
     protocol: TCP
---
apiVersion: extensions/v1beta1
kind: Deployment
metadata:
  name: quotesgateway
  namespace: challengelinkerd
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
        name: quotesgateway
        app: gateway
        version: v1
    spec:
      containers:
      - name: quotesgateway
        image: csaocpger/quotesgateway:3
        ports:
        - containerPort: 3000
          name: http
          protocol: TCP
        env: 
        - name: "GATEWAY_PORT"
          value: "3000"
        - name: "GATEWAY_QUOTES_URL"
          value: "http://quotes-backend:3000/api"
```

Create secret with SPA endpoint:

Get gateway external IP:

```shell
$ kubectl get svc -n challengelinkerd
NAME               TYPE           CLUSTER-IP    EXTERNAL-IP   PORT(S)        AGE
quotes-backend     ClusterIP      10.0.37.62    <none>        3000/TCP       15m
quotesgatewaysvc   LoadBalancer   10.0.20.244   40.118.28.7   80:31092/TCP   15m
```

Adjust settings_template.js file in apps/quotes-frontend/public/settings (IP with *dashes*!):

```js
var uisettings = {
    "endpoint": "http://40-118-28-7.nip.io/quotes"
}
```

```shell
$ kubectl create secret generic uisettings --from-file=settings.js=./settings.js -n challengelinkerd
```

FRONTEND

```yaml
apiVersion: v1
kind: Service
metadata:
  name: quotesfrontendsvc
  namespace: challengelinkerd
  labels:
    name: quotesfrontendsvc
    app: frontend
spec:
  type: LoadBalancer
  selector:
    app: frontend
  ports:
   - port: 80
     name: http
     targetPort: 80
     protocol: TCP
---
apiVersion: extensions/v1beta1
kind: Deployment
metadata:
  name: quotesfrontend
  namespace: challengelinkerd
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
        name: quotesfrontend
        app: frontend
        version: v1
    spec:
      containers:
      - name: quotesfrontend
        image: csaocpger/quotesfrontend:4
        volumeMounts:
          - mountPath: "/usr/share/nginx/html/settings"
            name: uisettings
            readOnly: true
      volumes:
      - name: uisettings
        secret:
          secretName: uisettings
```

## Explore linkerd ##

Open your browser and point to frontendservice IP

```shell
$ linkerd dashboard
```

## Service Profiles ##

Generate a service profile from watching current traffic in your cluster.

```shell
$ linkerd profile -n challengelinkerd quotes-backend --tap deploy/quotesbackend --tap-duration 10s
```

You will receive output like that:

```yaml
apiVersion: linkerd.io/v1alpha2
kind: ServiceProfile
metadata:
  creationTimestamp: null
  name: quotes-backend.challengelinkerd.svc.cluster.local
  namespace: challengelinkerd
spec:
  routes:
  - condition:
      method: GET
      pathRegex: /api/quotes
    name: GET /api/quotes
```

Apply that yaml to your cluster and make your service "available" for linkerd.

### Configure retries ###

When you run the application in "loop mode", you will see errors appear once in a while. Linkerd is able to retry idempotent request. So add *isRetryable* property to your service profile.

```yaml
apiVersion: linkerd.io/v1alpha2
kind: ServiceProfile
metadata:
  creationTimestamp: null
  name: quotes-backend.challengelinkerd.svc.cluster.local
  namespace: challengelinkerd
spec:
  routes:
  - condition:
      method: GET
      pathRegex: /api/quotes
    name: GET /api/quotes
    isRetryable: true
```

Again apply that configuration to your cluster and see the errors disappear, because linkerd will retry requests that fail. Your app doesn't have to deal with errors anymore.