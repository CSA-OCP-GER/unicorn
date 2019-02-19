# New Service Versions with Request Mirroring #

Base destination rule and virtual service

c2-destination-rule-base
c2-ingress-rr-base

Deploy V3 version jscalbackend-new:2.0

c2-jscalcbackend-new

Add destination rule for v3

c2-destination-rule-v3

Adjust virtual service for mirroring

c2-ingress-rr-mirroring

Open grafana dashboard and compare v1 against v3

![Istio Service Mesh](/img/grafana_compare.png)

Also check in the browser, that no requests are served to the frontend with service `v3`.