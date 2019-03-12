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

## Deploy Sample Application ##

## Create Ingress ##

## DNS / Cert-Manager / Let's Encrypt Certificate ##

## IP-Whitelisting ##

## Rate Limiting ##

## Basic Authentication ##

## External Authentication / Azure Active Directory ##

