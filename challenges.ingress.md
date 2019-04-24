# Advanced Ingress #

> Need help in this challenge? Check deployment files [here :blue_book:](hints/yaml/challenge-ingress)!

## Here is what you will learn ##

- Deploy an ingress controller
- Define ingress rules
- IP Whitelisting to limit access to certain applications
- Achieve rate limiting with an ingress controller
- Enable Basic Auth
- Delegate Authentication to Azure Active Directory and learn how to integrate Let's Encrypt

## Kubernetes Ingress Controller ##

Kubernetes handles "east-west" or "cluster-internal" communication by assigning IP addresses to services / pods which can be reached cluster-wide. When it comes to "north-south" or external connectivity, there are several ways to achieve this. 

First, you can assign a public IP to a node and define a `NodePort` service type (not really recommended). 

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

First, deploy some sample application we can use to demonstrate the inress features. We also use Helm for that.

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

Now that our sample applications are running (but still aren't reachable via the internet), we can create ingress definitions for each of the application. We will demonstrate this by using `Domain-based routing` (you can also use e.g. path-based routing. You will see an example later when integrating AAD to login to an application).

> **Optional, but highly recommended:** for domain-based routing, you need a custom domain, you can use (and manage). If you don't have a domain, you can create a domain at a free domain provider for testing purposes. There are several services out there you can use, e.g. https://www.ddnss.de (supports wildcards - we will need this feature!) or https://www.noip.com (doesn't support wildcards in the "free" plan). All you need to do is register a domain and point the *A-Record* to the IP address of your ingress controller. If you don't want to create an account at one of these providers, you can go through all of the samples...but you won't be able to complete this challenge.

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

Our first example that leverages the features of the NGINX ingress controller, is access-limitation to our application by whitelisting IP address(-ranges). 

First, get the public IP-address of your machine (e.g. by opening the following web page: https://www.whatismyip.com/).

Adjust the ingress definition for the "Stranger-Things"-voting app to limit the access to this IP address.

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

Visit the site with your browser and again with your mobile device (preferrably NOT via wireless connection :smile:). In one of the cases, you will be blocked!

## Rate Limiting ##

**Rate Limiting** is used to control the amount of traffic a client creates for your services. You can add annotations to your ingress definitions to influence the behavior of NGINX.

In this sample, we rate-limit one of our services with the value of "10 requests per minute". Other definitions can be found here: https://kubernetes.github.io/ingress-nginx/user-guide/nginx-configuration/annotations/#rate-limiting 

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

> You can also "whitelist" certain IP addresses/address ranges that will be excluded for this setting via `nginx.ingress.kubernetes.io/limit-whitelist` annotation.

Apply the ingress definition and try to load the service a few times in your browser. See how the ingress controller limits your requests.

## Basic Authentication ##

Now, we will protect one of our applications by using `basic-authentication` (username/password).

### Pre-Requisites ###
Create a `auth` file: http://www.htaccesstools.com/htpasswd-generator/ or via...

```shell
$ htpasswd -c ./auth user1
(You will be promted to enter a password)

$ htpasswd ./auth user2
```

> **INFO:** It is important, that you name you file `auth`!

Add the file as Kubernetes `secret`:

```shell
$ kubectl create secret generic basic-auth --from-file=auth -n ingress-samples
```

### Apply the configuration ###

Now add the corresponding annotains that pick-up the `auth` file and enforce basic authentication on our ingress:

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

Again, check your browser! Hopefully, you will be prompted to enter a username and password! :smile:

## External Authentication / Azure Active Directory / Cert-Manager / Let's Encrypt Certificate ##

In this section, we are going to enable authentication via Azure Active Directory. Therefore, we first need to register an application in our directory that represents the actual application.

Follow these steps:

Go to the portal and under "Azure Active Directory", click on "App Registrations (Preview)".

> If you created a directory for this workshop, use that AAD.

![App Registrations](/img/ingress-app-reg1.png)

Then register a new app.

![App Registrations](/img/ingress-app-reg2.png)

In the "Redirect" section, enter the following URL (we will need that later):

*https://<subdomain.domain>/oauth2/callback* (e.g. https://headers.project-unicorn.ddnss.de/oauth2/callback)

Click on "Register".

When the application has been registered, create a client secret (we will also need that later):

![App Registrations](/img/ingress-app-reg4.png)

Copy the secret for later use.

To be able to reach our application via https/SSL, we will leverage the "Let's Encrypt" service, that will give us free SSL certificates for our site.

There is a very convienient way to create certifictes "on the fly" for our ingress definitions. Jetstack has created a Kubernetes addon called "Certmanager", that will automatically provision and manage TLS certificates in Kubernetes (Docs: https://docs.cert-manager.io/en/latest/). It will ensure certificates are valid and attempt to renew certificates at an appropriate time before expiration.

So, let's install that addon in our Kubernetes cluster.

### Install Cert-Manager ###

We will use Helm to install the cert-manager.

```shell
$ kubectl apply -f https://raw.githubusercontent.com/jetstack/cert-manager/release-0.7/deploy/manifests/00-crds.yaml

$ kubectl label namespace ingress-samples certmanager.k8s.io/disable-validation=true

$ helm repo add jetstack https://charts.jetstack.io

$ helm repo update

$ helm install stable/cert-manager --name cert-manager \
  --set ingressShim.defaultIssuerName=letsencrypt-prod \
  --set ingressShim.defaultIssuerKind=ClusterIssuer --namespace ingress-samples
```

Now that cert-manager is installed, we need to add a few Kubernetes definitions to our cluster, to be able to request SSL certificates from *Let's Encrypt*.

### Certificate Issuer ###

Register a certificate issuer for our cluster. Replace `<EMAIL>` with your email address.

```yaml
apiVersion: certmanager.k8s.io/v1alpha1
kind: ClusterIssuer
metadata:
  name: letsencrypt-prod
  namespace: ingress-samples
spec:
  acme:
    server: https://acme-v02.api.letsencrypt.org/directory
    email: <EMAIL>
    privateKeySecretRef:
      name: letsencrypt-prod
    http01: {}
```

### Create Domain Certificate ###

Register a certificate for your domain. Replace `<DOMAIN>` with the domain, you registered at the DNS service, e.g. *project-unicorn.ddnss.de*.

```yaml
apiVersion: certmanager.k8s.io/v1alpha1
kind: Certificate
metadata:
  name: domain-cert
  namespace: ingress-samples
spec:
  secretName: tls-secret
  dnsNames:
  - <DOMAIN>
  acme:
    config:
    - http01:
        ingressClass: nginx
      domains:
      - <DOMAIN>
  issuerRef:
    name: letsencrypt-prod
    kind: ClusterIssuer
```

### Deploy an Auth-Proxy ###

Now that we are set to create certificates for our ingress definitions, let's install the authorization proxy that will take care of login us in and check, if a user has a valid session, before directing traffic to our services.

There is a project called "Oauth-Proxy2" (just one of many out there) that does exactly what we want to achieve.

You will need to adjust the following parameters:

- `<TENANT_ID>` - your Azure tenant ID
- `<APPLICATION_ID>` - the ID of the application you created in AAD
- `<APPLICATION_KEY>` - the client secret you created for the AAD application
- `<BASE64_ENCODED_CUSTOM_SECRET>` - create a secret like "mysecret123!" and encode it as a base64 string. (`echo  'mysecret123!' | base64` or at https://www.base64encode.org/)

```yaml
apiVersion: extensions/v1beta1
kind: Deployment
metadata:
  labels:
    application: oauth2-proxy
  name: oauth2-proxy-deployment
  namespace: ingress-samples
spec:
  replicas: 1
  selector:
    matchLabels:
      application: oauth2-proxy
  template:
    metadata:
      labels:
        application: oauth2-proxy
    spec:
      containers:
      - args:
        - --provider=azure
        - --azure-tenant=<TENANT_ID>
        - --pass-access-token=true
        - --cookie-name=_mycookie
        - --email-domain=*
        - --upstream=file:///dev/null
        - --http-address=0.0.0.0:4180
        name: oauth2-proxy
        image: a5huynh/oauth2_proxy:2.2
        env:
        - name: OAUTH2_PROXY_CLIENT_ID
          value: <APPLICATION_ID>
        - name: OAUTH2_PROXY_CLIENT_SECRET
          value: <APPLICATION_KEY>
        - name: OAUTH2_PROXY_COOKIE_SECRET
          value: <BASE64_ENCODED_CUSTOM_SECRET>
        ports:
        - containerPort: 4180
          protocol: TCP
---
apiVersion: v1
kind: Service
metadata:
  labels:
    application: oauth2-proxy
  name: oauth2-proxy-svc
  namespace: ingress-samples
spec:
  ports:
  - name: http
    port: 4180
    protocol: TCP
    targetPort: 4180
  selector:
    application: oauth2-proxy
```

Now, everything is in place to add another service that we want to protect via the OAuth proxy and Azure Active Directory.

### Sample app to protect ###

First, deploy a sample application with a Kubernetes service that should be protected.

```yaml
apiVersion: extensions/v1beta1
kind: Deployment
metadata:
  name: web-deploy
  labels:
    application: web
    tier: frontend
  namespace: ingress-samples
spec:
  replicas: 1
  strategy:
    type: RollingUpdate
    rollingUpdate:
      maxSurge: 1
      maxUnavailable: 1
  minReadySeconds: 5
  revisionHistoryLimit: 3
  selector:
    matchLabels:
      application: web
      tier: frontend
  template:
    metadata:
      labels:
        application: web
        tier: frontend
    spec:
      containers:
        - name: frontend
          image: csaocpger/headertester:1.0
          ports:
            - containerPort: 3000
---
apiVersion: v1
kind: Service
metadata:
  name: headers-svc
  labels:
    application: web
    tier: frontend
  namespace: ingress-samples
spec:
  selector:
    application: web
    tier: frontend
  ports:
    - port: 80
      targetPort: 3000
```

The sample is really simple: the application just shows all http headers that have been sent to it on a webpage.

![App Registrations](/img/ingress-headers.png)

### Finally, the Ingress ###

Now, let's wire up everything. We create two ingress definitions:

- one for the OAuth-Proxy on path `/oauth`
- one for the application itself on root `/`

Please replace `<SUBDOMAIN.YOUR_DOMAIN>` the URL you want to use, e.g. headers.project-unicorn.ddnss.de.

```yaml
apiVersion: extensions/v1beta1
kind: Ingress
metadata:
  annotations:
    kubernetes.io/ingress.class: nginx
    nginx.ingress.kubernetes.io/ssl-redirect: "true"
    certmanager.k8s.io/cluster-issuer: letsencrypt-prod
  name: oauth2-ingress
  namespace: ingress-samples
spec:
  rules:
    - host: <SUBDOMAIN.YOUR_DOMAIN>
      http:
        paths:
          - path: /oauth2
            backend:
              serviceName: oauth2-proxy-svc
              servicePort: 4180
  tls:
    - hosts:
        - <SUBDOMAIN.YOUR_DOMAIN>
      secretName: tls-secret
---
apiVersion: extensions/v1beta1
kind: Ingress
metadata:
  name: headers-ing
  namespace: ingress-samples
  annotations:
    kubernetes.io/ingress.class: "nginx"
    nginx.ingress.kubernetes.io/ssl-redirect: "true"
    nginx.ingress.kubernetes.io/rewrite-target: /
    certmanager.k8s.io/cluster-issuer: letsencrypt-prod
    nginx.ingress.kubernetes.io/auth-url: "https://$host/oauth2/auth"
    nginx.ingress.kubernetes.io/auth-signin: "https://$host/oauth2/start?rd=$request_uri"
spec:
  tls:
    - hosts:
        - <SUBDOMAIN.YOUR_DOMAIN>
      secretName: tls-secret
  rules:
    - host: <SUBDOMAIN.YOUR_DOMAIN>
      http:
        paths:
          - path: /
            backend:
              serviceName: headers-svc
              servicePort: 80
```

A few notes on the two definitions. 

If you look at the annotations of the first ingress, you can see that we tell NGINX to automatically rewrite `http` traffic to `https` (`nginx.ingress.kubernetes.io/ssl-redirect: "true"`). We also request a SSL certificate for the `spec.tls.hosts` entry by using `certmanager.k8s.io/cluster-issuer: letsencrypt-prod` (where we tell cert-manager to use our `ClusterIssuer` 'lets-encrypt' which in turn will request the certificate and put it in the `secret` called 'tls-secret').

Let's have a look at the second ingress definition. There are two new annotations:

- `nginx.ingress.kubernetes.io/auth-url` - this represents the endpoint where NGINX is checking, if a user has a valid session
- `nginx.ingress.kubernetes.io/auth-signin` - this is the signin URL of our application. OAuth2-Proxy then redirects to the Azure Active Directory login endpoint

Having all components in place now, let's try to access the application in the browser. You will see that you will be redirected to Azure Active Directory...what we wanted to achieve.

![AAD Login](/img/ingress-aad-login.png)
