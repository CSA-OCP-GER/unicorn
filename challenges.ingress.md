# Advanced Ingress #

## Here is what you will learn ##

- Deploy an ingress controller
- Define ingress rules
- Learn how to integrate Let's Encrypt certs
- IP Whitelisting to limit access to certain applications
- Achieve rate limiting with an ingress controller
- Enable Basic Auth
- Delegate Authentication to Azure Active Directory

## Kubernetes Ingress Controller ##

Kubernetes handles "east-west" or "cluster-internal" communication by assigning IP addresses to services / pods which can be reached cluster-wide. When it comes to "north-south" or external connectivity, there are several ways to achieve this. 

First, you can assign a public IP to a node and define a `NodePort` service type (not recommended). 

![NodePort](/img/ingress_nodeport.png)

For platforms like Azure, you can create a service of type `LoadBalancer` that requests a public IP address at the platform loadbalancer which in turn routes traffic to the Kubernetes service. 

![LoadBalancer](/img/ingress_loadbalancer.png)

Lastly - and this is the recommended and most flexible way - you can define `Ingress` resources that will be handled by an *Ingress Controller*. 

![IngressController](/img/ingress_controller.png)

An Ingress Controller can sit in front of many services within a cluster, routing traffic to them and - depending on the implementation - can also add functionality like SSL termination, path rewrites, or name based virtual hosts. 

An `ingress` is a core concept of Kubernetes, but is always implemented by a third party proxy. There are many implementations out there (Kong, Ambassador, Traeffic etc.), but the currently most seen solution is the [NGINX Ingress Controller](https://kubernetes.github.io/ingress-nginx/).


## Deploy Ingress Controller ##

To demonstrate the features of an ingress controller, we create a new namespace where our sample applications will be deployed to.

```shell
$ kubectl create ns ingress-samples
```

Now, let's start by deploying the NGINX ingress controller to our cluster! The most convienient way is to use a preconfigured [Helm chart](https://github.com/helm/charts/tree/master/stable/nginx-ingress).

```shell
helm install stable/nginx-ingress --name clstr-ingress --set rbac.create=true,controller.scope.enabled=true,controller.scope.namespace=ingress-samples,controller.service.externalTrafficPolicy=Local --namespace ingress-samples
```

> **Info:** we limit the scope of our ingress controller to a certain namespace (*ingress-samples*). In production environments, it is a good practices to not share an ingress controller for multiple environments. NGINX configuration quickly grows up to thousands of lines and suddenly starts to have config reload issues! Therefore, try to keep things clean, give each environment its own controller and avoid such problems from the beginning.

## Deploy Sample Applications ##

```shell
$ helm repo add azure-samples https://azure-samples.github.io/helm-charts/

$ helm install azure-samples/azure-vote --set title="Winter Is Coming?" --set value1="Jon and Daenerys say YES" --set value2="Cersei says NO" --set serviceName=got-vote --set serviceType=ClusterIP --set serviceNameFront=got-vote-front --name got-vote --namespace ingress-samples
```

![GOT Voting](/img/ingress_got_vote.png)

*GOT Voting App*

```shell
$ helm install azure-samples/azure-vote --set title="Is the Hawkins National Laboratory a safe place?" --set value1="Will Byers says no" --set value2="Eleven says NOOOOOOO!!" --set serviceName=stranger-things-vote --set serviceType=ClusterIP --set serviceNameFront=stranger-things-vote-front --name stranger-things-vote --namespace ingress-samples
```

![Stranger Things Voting](/img/ingress_st_vote.png)

*Stranger Things Voting App*

We deploy both applications with a frontend service of type `ClusterIP`, because we are going to deploy `ingress` resources for each voting app and skip creating public IP addresses for each frontend service.

## Create Ingress ##

Now that our sample applications are running (but still aren't reachable via the internet), we can create ingress definitions for each of the application. We will show two approaches:

- Domain-based routing (sample)
- Path based routing

> **Optional:** for domain-based routing, you need a custom domain, you can use (and manage). If you don't have a domain, you can create a domain at a free domain provider for testing purposes. There are several services out there you can use, e.g. https://www.ddnss.de (supports wildcards) or https://www.noip.com (doesn't support wildcards). All you need to do is register a domain and point the *A-Record* to the IP address of your ingress controller. If you don't want to create an account at one of these providers, you can go through all of the samples...but some, you won't be able to complete.

So, let's create ingress resources for our two applications:

*Game of Thrones Vote*
```yaml
apiVersion: extensions/v1beta1
kind: Ingress
metadata:
  name: got-ing
  namespace: ingress-samples
  annotations:
    kubernetes.io/ingress.class: "nginx"
    nginx.ingress.kubernetes.io/rewrite-target: /
spec:
  rules:
    - host: got.<YOUR_CUSTOM_DNS_NAME>
      http:
        paths:
          - path: /
            backend:
              serviceName: got-vote-front
              servicePort: 80
```

*Stranger Things Vote*
```yaml
apiVersion: extensions/v1beta1
kind: Ingress
metadata:
  name: stranger-things-ing
  namespace: ingress-samples
  annotations:
    kubernetes.io/ingress.class: "nginx"
    nginx.ingress.kubernetes.io/rewrite-target: /
spec:
  rules:
    - host: stranger-things.<YOUR_CUSTOM_DNS_NAME>
      http:
        paths:
          - path: /
            backend:
              serviceName: stranger-things-vote-front
              servicePort: 80
```
Open your browser an test the ingress definitions. You should be corretly routed to your applications.

## IP-Whitelisting ##

Browser: https://www.whatismyip.com/

```yaml
apiVersion: extensions/v1beta1
kind: Ingress
metadata:
  name: stranger-things-ing
  namespace: ingress-samples
  annotations:
    kubernetes.io/ingress.class: "nginx"
    nginx.ingress.kubernetes.io/rewrite-target: /
    nginx.ingress.kubernetes.io/whitelist-source-range: "YOUR-IP"
spec:
  rules:
    - host: stranger-things.<YOUR_CUSTOM_DNS_NAME>
      http:
        paths:
          - path: /
            backend:
              serviceName: stranger-things-vote-front
              servicePort: 80

```

Visit the site with your browser and again with your mobile device (preferrably NOT via wireless connection :smile:).

## Rate Limiting ##

```yaml
apiVersion: extensions/v1beta1
kind: Ingress
metadata:
  name: stranger-things-ing
  namespace: ingress-samples
  annotations:
    kubernetes.io/ingress.class: "nginx"
    nginx.ingress.kubernetes.io/rewrite-target: /
    nginx.ingress.kubernetes.io/limit-rpm: "10"
spec:
  rules:
    - host: stranger-things.<YOUR_CUSTOM_DNS_NAME>
      http:
        paths:
          - path: /
            backend:
              serviceName: stranger-things-vote-front
              servicePort: 80
```

> You can "whitelist" certain IP addresses/address ranges that will be excluded for this setting via `nginx.ingress.kubernetes.io/limit-whitelist` annotation.

## Basic Authentication ##

Create a `.htpasswd` file: http://www.htaccesstools.com/htpasswd-generator/ or via 

```shell
$ htpasswd -c ./.htpasswd user1
(You will be promted to enter a password)

$ htpasswd ./.htpasswd user2
```

Add the file as `secret`:

```shell
$ kubectl create secret generic basic-auth --from-file=.htpasswd -n ingress-samples
```

```yaml
apiVersion: extensions/v1beta1
kind: Ingress
metadata:
  name: stranger-things-ing
  namespace: ingress-samples
  annotations:
    kubernetes.io/ingress.class: "nginx"
    nginx.ingress.kubernetes.io/rewrite-target: /
    nginx.ingress.kubernetes.io/auth-type: basic
    nginx.ingress.kubernetes.io/auth-secret: basic-auth
    nginx.ingress.kubernetes.io/auth-realm: "The Demogorgon says: Authentication Required"
spec:
  rules:
    - host: stranger-things.<YOUR_CUSTOM_DNS_NAME>
      http:
        paths:
          - path: /
            backend:
              serviceName: stranger-things-vote-front
              servicePort: 80
```

## External Authentication / Azure Active Directory ##

## DNS / Cert-Manager / Let's Encrypt Certificate ##
