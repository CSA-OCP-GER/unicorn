# Implement Monitoring / Tracing #

When operating a microservice application, you quickly come to the point, where you want to have some insights of the services running in your cluster. With Istio / Kubernetes, there are several options to achive this. 

- Azure Application Insights / Azure Monitor
  - we do not cover that topic in this workshop. If you want to learn more about these options, have a look at the predecessor of this workshop (Chapter 4): https://github.com/CSA-OCP-GER/phoenix/blob/master/challenges.4.md
- Prometheus
  - open-source systems monitoring and alerting toolkit
- Grafana
  - open source metric analytics & visualization suite
- Kiali
  - new kid on the block :) built with Istio service-mesh in mind

In our example, we are going to use the "built-in" options Prometheus/Grafan and Kiali.

## Here is what you will learn ##

- Install Prometheus/Grafana components in your cluster
- Learn how to use the Grafana Dashboard
- Install Kiali service in your cluster
- Learn how to use the Kialia Graph
- Gain insights about "the moving parts" your service mesh

## Enable Grafana / Prometheus ##

> If you have installed Istio with the "DEFAULT" profile, you need to do the following steps. Otherwise (profile "DEMO"), Grafana is already installed and ready to use. Just move on to **Access Grafana**

We installed Istio with a Helm chart that already contains all the information/configuration to run Prometheus/Grafana...it just isn't installed by default. So we need to adjust the `values.yaml` file to fit our needs. 

Open `values.yaml` under `install/kubernetes/helm/istio` and adjust the following values:

```yaml
grafana:
  enabled: true
``` 
> You can expose Grafana via Service Type `LoadBalancer`, but: It is NOT recommended !! In production environments, use at least a custom ingress definition with e.g. IP Whitelisting

When finished editing, upgrade your Istio release:

```shell
$ helm upgrade istio install/kubernetes/helm/istio -f .\install\kubernetes\helm\istio\values.yaml
```

Now wait approximately one minute.

```shell
$ kubectl get po -n istio-system

NAME                                     READY     STATUS      RESTARTS   AGE
grafana-c49f9df64-8x574                  1/1       Running     0          20h
istio-citadel-7f699dc8c8-x2p8z           1/1       Running     0          20h
istio-egressgateway-54f556bc5c-2qqmc     1/1       Running     0          20h
istio-galley-687664875b-ldc2s            1/1       Running     0          20h
istio-ingressgateway-688d5886d-q8fq6     1/1       Running     0          20h
istio-init-crd-10-mp565                  0/1       Completed   0          20h
istio-init-crd-11-mq926                  0/1       Completed   0          20h
istio-pilot-66964dfcd6-dfhdl             2/2       Running     0          20h
istio-policy-5bccd487c8-5gcc8            2/2       Running     2          20h
istio-sidecar-injector-d48786c5c-7vzzf   1/1       Running     0          20h
istio-telemetry-59794cc5b4-cqzxx         2/2       Running     2          20h
istio-tracing-79db5954f-kz2fr            1/1       Running     0          20h
kiali-5c4cdbb869-f75xm                   1/1       Running     0          20h
prometheus-67599bf55b-wgnr6              1/1       Running     0          20h
```

### Access Grafana ### 

Because we did not expose the Grafana Dashboard to the public internet, we need to access Grafana via port-forwarding:

```shell
$ kubectl port-forward <GRAFANA-POD-NAME> 3000:3000 -n istio-system
```

Open your browser on http://localhost:3000. 

### Todos for you ###

First, run the application in **loop-mode** and put some load on your cluster.

Now...
- get familiar with the Grafana Dashboard and the preconfigured charts (under `Home` dropdown > Istio dashboards)
- see how your services are consuming CPU / memory / disk resources
- measure incoming requests
- "deep-dive" into measures of one service

![Grafana Service Dashboard](/img/grafana_service.png)
*Sample Dashboard*


## Enable Kiali ##

> If you have installed Istio with the "DEFAULT" profile, you need to do the following steps. Otherwise (profile "DEMO"), Kiali is already installed and ready to use. Just move on to **Access Kiali**

As mentioned above, Kiali is a new solution that is designed especially for Istio. It can answer the following questions:

- what services are part of my Istio service mesh?
- how are they connected?
- how do they perform?
- are there any errors in communication?

To be able to use Kiali, we need to install/enable it as we did with the Grafana dashboard.

So, first, edit the `values.yaml` file and adjust the following properties:

```yaml
kiali:
  enabled: true
```

> If you want to, also adjust username and password for the Kiali admin user.

Now, upgrade the Istio release:

```shell
$ helm upgrade istio install/kubernetes/helm/istio -f .\install\kubernetes\helm\istio\values.yaml
```

### Access Kiali ###

To be able to open the Kiali, execute the following command:

```shell
$ kubectl get po -n istio-system
```
Use the name of the pod running Kiali in the next command.

```shell
$ kubectl port-forward <KIALI_POD_NAME> 20001:20001 -n istio-system
```

Now, open your browser at http://localhost:20001/kiali/console/overview and login with the standard credentials (admin/admin).

### Todos for you ###

First, run the application in **loop-mode** and put some load on your cluster.

Now...
- get familiar with Kiali (open *Application*, *Workload* and *Services* views) and see, what metrics and information Kiali can provide for your service mesh
- open the service map under *Graph* and display *Service Nodes* and *Traffic Animation*
- get detailed information about services by clicking on a node in the graph

![Kiali Overview](/img/kiali_overview.png)

*Overview*

![Kiali Service Map](/img/kiali_service_map.png)

*Kiali Service Map*

![Kiali Service Info](/img/kiali_service_info.png)

*Kiali Service Info*