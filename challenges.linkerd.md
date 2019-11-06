# Linkerd #

## Introduction to Linkerd ##

...

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

```yaml
apiVersion: v1
kind: Service
metadata:
  name: calcfrontendsvc
  namespace: challengelinkerd
  labels:
    name: calcfrontendsvc
    app: frontend
spec:
  type: LoadBalancer
  selector:
    app: frontend
  ports:
   - port: 80
     name: http-calcfrontend
     targetPort: 80
     protocol: TCP
---
apiVersion: extensions/v1beta1
kind: Deployment
metadata:
  name: jscalcfrontend-v2
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
        name: jscalcfrontend
        app: frontend
        version: v2
    spec:
      containers:
      - name: jscalcfrontend
        image: csaocpger/jscalcfrontend:5.0
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
apiVersion: v1
kind: Service
metadata:
  name: calcbackendsvc
  namespace: challengelinkerd
  labels:
    name: calcbackendsvc
    app: backend 
spec:
  ports:
  - port: 80
    name: http-calcbackend
    targetPort: 80
  selector:
    app: backend
---
apiVersion: extensions/v1beta1
kind: Deployment
metadata:
  name: jscalcbackend-v1
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
        name: jscalcbackend
        app: backend
        version: v1
    spec:
      containers:
      - name: calcbackend
        image: csaocpger/jscalcbackend:2.0
        ports:
          - containerPort: 80
            name: http
            protocol: TCP
        env: 
          - name: "PORT"
            value: "80"
---
apiVersion: extensions/v1beta1
kind: Deployment
metadata:
  name: gocalcbackend-v1
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
        name: gocalcbackend
        app: backend
        version: v1
    spec:
      containers:
      - name: gocalcbackend
        image: csaocpger/gocalcbackend:1.0
        ports:
          - containerPort: 80
            name: http
            protocol: TCP
        env: 
          - name: "PORT"
            value: "80"
---
apiVersion: extensions/v1beta1
kind: Deployment
metadata:
  name: netcorecalcbackend-v1
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
        name: netcorecalcbackend
        app: backend
        version: v1
    spec:
      containers:
      - name: netcorecalcbackend
        image: csaocpger/netcorecalcbackend:1.0
        ports:
          - containerPort: 80
            name: http
            protocol: TCP
        env:
          - name: "PORT"
            value: "80"
```

## Explore linkerd ##

Open your browser and point to frontendservice IP

```shell
$ linkerd dashboard
```

## Service Profiles ##

```shell
$ linkerd profile -n challengelinkerd cbserviceprofile --tap deploy/gocalcbackend-v1 --tap-duration 10s > cbserviceprofile.yaml
```

### Configure retries ###

#### Add some Choas ####

First, service account that is able to kill pods...

```yaml
apiVersion: v1
kind: ServiceAccount
metadata:
  name: chaos
  namespace: challengelinkerd
---
apiVersion: rbac.authorization.k8s.io/v1
kind: RoleBinding
metadata:
  creationTimestamp: null
  name: chaos-admin
  namespace: challengelinkerd
roleRef:
  apiGroup: rbac.authorization.k8s.io
  kind: ClusterRole
  name: admin
subjects:
- kind: ServiceAccount
  name: chaos
  namespace: challengelinkerd
```

Chaos pod (kills backend pods in current namespace)

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  labels:
    run: chaos
  name: chaos
  namespace: challengelinkerd
spec:
  replicas: 1
  selector:
    matchLabels:
      run: chaos
  template:
    metadata:
      labels:
        run: chaos
    spec:
      containers:
      - image: csaocpger/chaos:4
        name: chaos
      serviceAccountName: chaos
      automountServiceAccountToken: true
```
