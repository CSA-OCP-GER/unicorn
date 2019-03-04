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

We installed Istio with a Helm chart that already contains all the information/configuration to run Prometheus/Grafana...it just isn't installed by default. So we need to adjust the `values.yaml` file to fit our needs. 

Open `values.yaml` under `install/kubernetes/helm/istio` and adjust the following values:

```yaml
grafana:
  enabled: true
...
...
grafana:
  service:
    type: LoadBalancer
``` 
> It is NOT recommended to expose Grafana Dashboard via a service of type `LoadBalancer`. In production environments, use at least a custom ingress definition with e.g. IP Whitelisting

When finished editing, upgrade your Istio release:

```shell
$ helm upgrade istio install/kubernetes/helm/istio -f .\install\kubernetes\helm\istio\values.yaml
```

Now wait approximately one minute for Kubernetes to receive a public IP address for the Grafana service. You can check the status via the following query (wait until `EXTERNAL-IP` gets a proper value for `grafana`)

```shell
$ kubectl get svc -n istio-system -w

NAME                     TYPE           CLUSTER-IP     EXTERNAL-IP     PORT(S)                  AGE
grafana                  LoadBalancer   10.0.146.159   <PENDING>       3000:30992/TCP           26d
istio-citadel            ClusterIP      10.0.182.132   <none>          8060/TCP,9093/TCP        27d
istio-egressgateway      ClusterIP      10.0.157.56    <none>          80/TCP,443/TCP           27d
...
...
prometheus               ClusterIP      10.0.18.114    <none>          9090/TCP                 27d
```

Open your browser on http://\<EXTERNAL-IP\>:3000. 

Todos for you: 

First, run the application in **loop-mode** and put some load on your cluster.

Now...
- get familiar with the Grafana Dashboard and the preconfigured charts (under `Home` dropdown > Istio dashboards)
- see how your services are consuming CPU / memory / disk resources
- measure incoming requests
- "deep-dive" into measures of one service

![Grafana Service Dashboard](/img/grafana_service.png)
*Sample Dashboard*


## Enable Kiali ##

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

To be able to open the Kiali, execute the following command:

```shell
$ kubectl get po -n istio-system
```
Use the name of the pod running Kiali in the next command.

```shell
$ kubectl port-forward <KIALI_POD_NAME> 20001:20001 -n istio-system
```

> In this case, we don't expose the service via LoadBalander. We just use port-forwarding to the running pod as another possibility to use internal K8s services.

Now, open your browser at http://localhost:20001/console/overview and login with the credentials you set in the `values.yaml` file.

Todos for you: 

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