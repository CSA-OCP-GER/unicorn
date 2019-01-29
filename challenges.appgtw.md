# Create a Cluster with ApplicationGateway WAF

## Here is what you learn ##

- Secure your Cluster with ApplicationGateway WAF
  - create an ApplicationGateway with WAF enabled
  - route traffic from public ApplicationGateway to your Cluster's internal LoadBalancer

- Cluster
  - create a Clsuter with Azure Advanced Container Network Integration (Azure CNI)
  - create an internal Ingress Controller
  - deploy your cluster with Azure ARM Templates
  - create a Service Principal for Kubernetes to manage Azure Resources

## Create a ResourceGroup for your Cluster ##

Open a shell and create a ResourceGroup

```Shell
> az group create --name <your-rg-name> --location <your-azure-location>
```

## Create an Azure Active Directory Service Principal

```Shell
> az ad sp create-for-rbac --name <your-spn-name> --skip-assignment true
```
Remember the 'appId' and 'password' from the output.

Get the ObjectId of your created ServicePrincipal and remember it.

```Shell
> az ad sp show --id <your-appId>
```

## Cluster Deployment with Azure ResourceGroup Deployment ##

To deploy the Cluster on Azure use the following [ARM Template](src/applicationgateway/deployment/akscluster.json).
Open a shell and go to the directory where the ARM Template is located. 

```Shell
> az group deployment create -g <your-resourcegroup-name> --template-file akscluster.json --parameters akscluster.parameters.json --parameters aksClusterName=<your-cluster-name> aksServicePrincipalAppId=<your-spn-appId> aksServicePrincipalClientSecret=<your-spn-password> aksServicePrincipalObjectId=<your-spn-objectId> aksDnsPrefix=<dns-prefix-name-of-your-choice>
``` 

Remember the ip address of the ApplicationGateway from the output.


## Get cluster credentials and switch context of kubectl ##

After your cluster is deployed get the cluster credentials to access the cluster with bubectl.

```Shell
> az aks get-credentials --name <your-cluster-name> --resource-group <your-resourcegroup-name>
```

Check the current context of kubectl.

```Shell
> kubectl config current-context
```

Switch context to your deployed cluster.

```Shell
> kubectl config set-context <your-clustername>
```

## Deploy an internal Ingress Controller to your Cluster ##

At this point we have an AKS Cluster deployed to Azure without a Load Balancer.
Now we have to deploy an internal Ingress Controller with a private IP Address.
The currently used VNET has an IP address range of 16.0.0.0/8 if you used the default value in akscluster.parameters.json.
The VNET is splitted into two subnets
    - kubesubnet with an IP address range of 16.0.0.0/16
    - appgwsubnet with an IP address range of 16.1.0.0/16

To deploy an internal Ingress Controller we need to use a free IP address in the range of the kubesubnet address range.
For tis example we use 16.0.255.1.

Create a file named internal-ingress.yaml using the following example manifest file.

```yaml
controller:
  service:
    loadBalancerIP: 16.0.255.1
    annotations:
      service.beta.kubernetes.io/azure-load-balancer-internal: "true"
```

For this example we use Helm to install an internal NGINX Ingress Controller.

If helm is not installed on your system execute the following [steps](https://docs.helm.sh/using_helm/#installing-helm).

Do the following to install Helm on a RBAC enabled Cluster.
Create a file named helm-rbac.yaml and copy in the following yaml.

```yaml
apiVersion: v1
kind: ServiceAccount
metadata:
  name: tiller
  namespace: kube-system
---
apiVersion: rbac.authorization.k8s.io/v1
kind: ClusterRoleBinding
metadata:
  name: tiller
roleRef:
  apiGroup: rbac.authorization.k8s.io
  kind: ClusterRole
  name: cluster-admin
subjects:
  - kind: ServiceAccount
    name: tiller
    namespace: kube-system
```

Create the service account and role binding with ```kubectl apply``` command.

```Shell
> kubectl apply -f helm-rbac.yaml
```

Initialize helm.

```Shell
helm init --service-account tiller
```

Install NGINX Ingress Controller using helm.

```Shell
> helm install stable/nginx-ingress --namespace kube-system -f .\internal-ingress.yaml --set controller.replicaCount=2
```

Check if the service for the controller is deployed using the ip address 16.0.255.1

```Shell
> kubectl get service -n kube-system
```

## Run demo applications ##

To see the ingress controller in action, let's run two demo applications in your AKS cluster. In this example, Helm is used to deploy two instances of a simple 'Hello world' application.
Before you can install the sample Helm charts, add the Azure samples repository to your Helm environment as follows:

```Shell
> helm repo add azure-samples https://azure-samples.github.io/helm-charts/
```

Create the first demo application from a Helm chart with the following command:

```Shell
helm install azure-samples/aks-helloworld
```

Now install a second instance of the demo application. For the second instance, you specify a new title so that the two applications are visually distinct. You also specify a unique service name:

```Shell
helm install azure-samples/aks-helloworld --set title="AKS Ingress Demo" --set serviceName="ingress-demo"
```

Both applications are now running on your Kubernetes cluster. To route traffic to each application, create a Kubernetes ingress resource. The ingress resource configures the rules that route traffic to one of the two applications. 
In the following example, traffic to the address http://16.0.255.1/ is routed to the service named aks-helloworld. Traffic to the address http://16.0.255.1/hello-world-two is routed to the ingress-demo service.
Create a file named hello-world-ingress.yaml and copy in the following example YAML.

```yaml
apiVersion: extensions/v1beta1
kind: Ingress
metadata:
  name: hello-world-ingress
  annotations:
    kubernetes.io/ingress.class: nginx
    nginx.ingress.kubernetes.io/ssl-redirect: "false"
    nginx.ingress.kubernetes.io/rewrite-target: /
spec:
  rules:
  - http:
      paths:
      - path: /
        backend:
          serviceName: aks-helloworld
          servicePort: 80
      - path: /hello-world-two
        backend:
          serviceName: ingress-demo
          servicePort: 80
```

Create the ingress resource using ```kubectl apply -f hello-world-ingress.yaml```.

```Shell
> kubectl apply -f hello-world-ingress.yaml
```

## Test the ingress controller ##

Now access the demo applications using your public Application Gateway ip.

```Shell
> curl -L <appgtw-ip>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head>
    <link rel="stylesheet" type="text/css" href="/static/default.css">
    <title>Welcome to Azure Kubernetes Service (AKS)</title>
[...]
```

Now add /hello-world-two path to the address, such as http://<appgtw-ip>/hello-world-two. The second demo application with the custom title is returned, as shown in the following condensed example output:

```
> curl -L -k http://<appgtw-ip>/hello-world-two

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head>
    <link rel="stylesheet" type="text/css" href="/static/default.css">
    <title>AKS Ingress Demo</title>
[...]
```