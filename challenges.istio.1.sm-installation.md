# Install Istio in your AKS cluster #

## Here is what you will learn ##

- Install Istio in your cluster
  - create Custom Resource Definitions
  - Configure Helm
  - deploy Istio to your cluster via Helm deployment
  - deploy base application

## Installation via Helm ##

Download Istio Release (1.1.3 / at the time of writing. Please stick to that version.): https://github.com/istio/istio/releases/tag/1.1.3

Unpack the archive to a folder underneath the Git repo.
<!-- 
### Install Custom Resource Definitions ###

Go to the directory where you unpacked Istio and run the following command.

```shell
$ kubectl apply -f install/kubernetes/helm/istio/templates/crds.yaml

customresourcedefinition.apiextensions.k8s.io "virtualservices.networking.istio.io" created
customresourcedefinition.apiextensions.k8s.io "destinationrules.networking.istio.io" created
[...]
customresourcedefinition.apiextensions.k8s.io "handlers.config.istio.io" created
``` -->

### Configure Helm/Tiller ###

Now configure the cluster to be able to use Helm for the deployment of Istio (create a service-account and CR-binding).

```shell
$ kubectl apply -f install/kubernetes/helm/helm-service-account.yaml

serviceaccount "tiller" created
clusterrolebinding.rbac.authorization.k8s.io "tiller" created

$ helm init --service-account tiller

[...]
```

### Install Istio via Helm Chart ###

First, install the Istio Custom Resource Definitions (CRDs)

```shell
$ helm install install/kubernetes/helm/istio-init --name istio-init --namespace istio-system
```

Check, that all CRDs have been installed successfully

```shell
$ kubectl get crds
```

Now it's time to install Istio with "demo" configuration onto your cluster (see https://istio.io/docs/setup/kubernetes/additional-setup/config-profiles/):

```shell
$ helm install install/kubernetes/helm/istio --name istio --namespace istio-system --values install/kubernetes/helm/istio/values-istio-demo.yaml

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

### Install a Kubernetes namespace for Challenge-Istio ###

```shell
$ kubectl create namespace challengeistio

Namespace challengeistio created.

## Label namespace to auto-inject istio sidecar during deployments
$ kubectl label namespace challengeistio istio-injection=enabled
```

### Install Base Sample App ###

This is the base sample application where all further deployments will depend on. It consists of the following pods/services (standard Kubernetes objects):

- Front end pod with the Angular application
- Frontend service pointing to these pods --> internal service that is **not** accessible via internet (external LoadBalancer)!
- Backend pods with the "business logic" (three implementations: Go, .NETCore & NodeJS)
- Backend service pointing to these pods --> internal service that is **not** accessible via internet (external LoadBalancer)!

```yaml
apiVersion: v1
kind: Service
metadata:
  name: calcfrontendsvc
  namespace: challengeistio
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
...
...
...
[and much more...]
```

```shell
$ kubectl apply -f hints/yaml/challengeistio/base-sample-app.yaml

service "calcfrontendsvc" created
deployment.extensions "jscalcfrontend-v2" created
service "calcbackendsvc" created
deployment.extensions "jscalcbackend-v1" created
deployment.extensions "gocalcbackend-v1" created
deployment.extensions "netcorecalcbackend-v1" created
```
