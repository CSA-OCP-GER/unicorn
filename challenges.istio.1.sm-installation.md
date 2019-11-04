# Install Istio in your AKS cluster #

## Here is what you will learn ##

- Install Istio in your cluster
  - create Custom Resource Definitions
  - Configure Helm
  - deploy Istio to your cluster via Helm deployment
  - deploy base application

## Installation ##

Download Istio Release (1.3.4 / at the time of writing. Please stick to that version.): https://github.com/istio/istio/releases/tag/1.3.4

Unpack the archive to a folder underneath the Git repo.

### Install Istio ###

First, install the Istio Custom Resource Definitions (CRDs)

```shell
$ kubectl apply -f install/kubernetes/helm/istio-init/files/
```

Check, that all CRDs have been installed successfully

```shell
$ kubectl get crds
```

Now it's time to install Istio with "demo" configuration onto your cluster (see https://istio.io/docs/setup/kubernetes/additional-setup/config-profiles/):

```shell
$ kubectl apply -f install/kubernetes/istio-demo.yaml

[...]
```

## Check Installation ##

```shell
$ kubectl get svc -n istio-system

NAME                     TYPE           CLUSTER-IP     EXTERNAL-IP   PORT(S)                                                                                                                                      AGE
grafana                  ClusterIP      10.0.171.17    <none>        3000/TCP                                                                                                                                     28s
istio-citadel            ClusterIP      10.0.170.169   <none>        8060/TCP,15014/TCP                                                                                                                           26s
istio-egressgateway      ClusterIP      10.0.219.107   <none>        80/TCP,443/TCP,15443/TCP                                                                                                                     28s
istio-galley             ClusterIP      10.0.218.14    <none>        443/TCP,15014/TCP,9901/TCP                                                                                                                   28s
istio-ingressgateway     LoadBalancer   10.0.95.107    <pending>     15020:30313/TCP,80:31380/TCP,443:31390/TCP,31400:31400/TCP,15029:32424/TCP,15030:32535/TCP,15031:32676/TCP,15032:32171/TCP,15443:32596/TCP   28s
istio-pilot              ClusterIP      10.0.118.30    <none>        15010/TCP,15011/TCP,8080/TCP,15014/TCP                                                                                                       27s
istio-policy             ClusterIP      10.0.253.232   <none>        9091/TCP,15004/TCP,15014/TCP                                                                                                                 27s
istio-sidecar-injector   ClusterIP      10.0.221.133   <none>        443/TCP,15014/TCP                                                                                                                            26s
istio-telemetry          ClusterIP      10.0.197.52    <none>        9091/TCP,15004/TCP,15014/TCP,42422/TCP                                                                                                       27s
jaeger-agent             ClusterIP      None           <none>        5775/UDP,6831/UDP,6832/UDP                                                                                                                   21s
jaeger-collector         ClusterIP      10.0.27.86     <none>        14267/TCP,14268/TCP                                                                                                                          22s
jaeger-query             ClusterIP      10.0.234.107   <none>        16686/TCP                                                                                                                                    22s
kiali                    ClusterIP      10.0.120.41    <none>        20001/TCP                                                                                                                                    28s
prometheus               ClusterIP      10.0.188.227   <none>        9090/TCP                                                                                                                                     26s
tracing                  ClusterIP      10.0.149.89    <none>        80/TCP                                                                                                                                       21s
zipkin                   ClusterIP      10.0.8.183     <none>        9411/TCP                                                                                                                                     21s
```

Check running pods:

```shell
$ kubectl get pods -n istio-system

NAME                                      READY   STATUS      RESTARTS   AGE
grafana-575c7c4784-6fk54                  1/1     Running     0          76s
istio-citadel-6cb95997f8-g5hzb            1/1     Running     0          73s
istio-egressgateway-6d4f69787b-bzz7p      1/1     Running     0          77s
istio-galley-b877d99f4-bspmq              1/1     Running     0          77s
istio-grafana-post-install-1.3.3-f98lt    0/1     Completed   0          84s
istio-ingressgateway-774f65f6f-m8fnz      0/1     Running     0          76s
istio-pilot-7f459bf88f-jxz5x              2/2     Running     0          74s
istio-policy-5bb5df64f6-gjw7k             2/2     Running     2          75s
istio-security-post-install-1.3.3-tbskr   0/1     Completed   0          82s
istio-sidecar-injector-6c65cfff5-hwd94    1/1     Running     0          73s
istio-telemetry-c8fdf6c46-tkv2m           2/2     Running     1          74s
istio-tracing-8456d6548f-b5jzg            1/1     Running     0          73s
kiali-7dd44f7696-ksndt                    1/1     Running     0          75s
prometheus-5679cb4dcd-6q88v               1/1     Running     0          73s

```

## Base Deployment for Sample Application ##

### Install a Kubernetes namespace for Challenge-Istio ###

```shell
$ kubectl create namespace challengeistio

Namespace challengeistio created.
```

```shell
## Label namespace to auto-inject istio sidecar during deployments
$ kubectl label namespace challengeistio istio-injection=enabled
```

### Install Base Sample App ###

This is the base sample application where all further deployments will depend on. It consists of the following pods/services (standard Kubernetes objects):

- Front end pod with the Angular application
- Frontend service pointing to these pods --> internal service that is **not** accessible via internet (no external LoadBalancer)!
- Backend pods with the "business logic" (three implementations: Go, .NETCore & NodeJS)
- Backend service pointing to these pods --> internal service that is **not** accessible via internet (no external LoadBalancer)!

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
$ kubectl apply -f hints/yaml/challenge-istio/base-sample-app.yaml

service "calcfrontendsvc" created
deployment.extensions "jscalcfrontend-v2" created
service "calcbackendsvc" created
deployment.extensions "jscalcbackend-v1" created
deployment.extensions "gocalcbackend-v1" created
deployment.extensions "netcorecalcbackend-v1" created
```
