# Install Istio in your AKS cluster #

## Installation via Helm ##

Download Istio Release (1.0.2): https://github.com/istio/istio/releases

### Install CRDS ###

```shell
$ kubectl apply -f install/kubernetes/helm/istio/templates/crds.yaml

customresourcedefinition.apiextensions.k8s.io "virtualservices.networking.istio.io" created
customresourcedefinition.apiextensions.k8s.io "destinationrules.networking.istio.io" created
[...]
customresourcedefinition.apiextensions.k8s.io "handlers.config.istio.io" created
```

### Configure Helm/Tiller ###

```shell
$ kubectl apply -f install/kubernetes/helm/helm-service-account.yaml

serviceaccount "tiller" created
clusterrolebinding.rbac.authorization.k8s.io "tiller" created

$ helm init --service-account tiller

[...]
```

### Install Istio via Helm Chart ###

```shell
$ helm install install/kubernetes/helm/istio --name istio --namespace istio-system

[...]
```

## Check Installation ##

```shell
$ kubectl get deployments -n istio-system

NAME                       DESIRED   CURRENT   UP-TO-DATE   AVAILABLE   AGE
istio-citadel              1         1         1            1           3m
istio-egressgateway        1         1         1            1           3m
istio-galley               1         1         1            1           3m
istio-ingressgateway       1         1         1            1           3m
istio-pilot                1         1         1            1           3m
istio-policy               1         1         1            1           3m
istio-sidecar-injector     1         1         1            1           3m
istio-statsd-prom-bridge   1         1         1            1           3m
istio-telemetry            1         1         1            1           3m
prometheus                 1         1         1            1           3m
```

## Base Deployment for Sample Application ##

### Install Namespace for Challenge 2 ###

```shell
$ kubectl create namespace challenge2

Namespace challenge2 created.

## Label namespace to auto-inject istio sidecar during deployments
$ kubectl label namespace challenge2 istio-injection=enabled
```

### Install Base Sample App ###

```yaml
apiVersion: v1
kind: Service
metadata:
  name: calcfrontendsvc
  namespace: challenge2
  labels:
    name: calcfrontendsvc
    app: frontend
spec:
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
...
...
...
[and much more...]
```

```shell
$ kubectl apply hints/yaml/challenge2/base-sample-app.yaml

service "calcfrontendsvc" created
deployment.extensions "jscalcfrontend-v2" created
service "calcbackendsvc" created
deployment.extensions "jscalcbackend-v1" created
deployment.extensions "gocalcbackend-v1" created
deployment.extensions "netcorecalcbackend-v1" created
```