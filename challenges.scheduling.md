# Advanced Scheduling / Resource Management #

Kubernetes has some advanced concepts when it comes to resource handling and scheduling of your workloads. In this challenge, you will learn about how scheduling works in Kubernetes and how you can influence which nodes will be selected and how pods can be prioritized. 

## Advanced Scheduling ##

The Kubernetes Scheduler is one of the master components that AKS manages for you. When it comes to a deployment of pods, the scheduler tries to find a node for each pod - one at a time. The algorithm works as follows:

1. The scheduler tries to filter out nodes that don't fit the request. There are several criterias Kubernetes checks. E.g. if a node can provide enough resources (computed as the capacity of the node minus the sum of the resource requests of the containers that are already running on the node), whether labels match or not, if there are disk conflicts (when it comes to mounting volumes), if requested host ports are available or not etc.
1. Nodes are then ranked by so-called "priority functions", e.g. `LeastRequestedPriority` ("least-loaded" metric), `BalancedResourceAllocation` (CPU/memory utilization is balanced after the pod scheduling), `CalculateNodeLabelPriority` (prefer nodes that have the specified label) etc. Each function gets a weight from 0-10 (whereas 10 means "most preferred"). Nodes with a higher value are then ranked higher.
1. Finally, the node with the highest priority is picked (if there are multiple nodes with the same rank, one is picked randomly).

### Node Affinity ###

With Node Affinity you can inluence they way Kubernetes picks nodes for scheduling your pod. 

Be aware, that there is also the concept of "node selectors" and that the feature discussed here is still in "beta". Nevertheless, it has some advantages over "node selectors":

- language is more feature-rich
- rules can be marked as "soft/preference", rather than being a hard requirement - that means, if a rule can't be satisfied the pod will still be scheduled

Currently, there are two types of node affinity:

1. `requiredDuringSchedulingIgnoredDuringExecution` - being the "hard" requirement
1. `preferredDuringSchedulingIgnoredDuringExecution` - being the "soft" requirement

"IgnoredDuringExecution" means that node labels bein changed at runtime won't affect pods currently running on that specific node.

#### Sample #### 

So first, let's check the current labels that are attached to the cluster nodes.

```shell
$ kubectl get nodes --show-labels
```

Now add some labels, we can use for scheduling with node affinity.

```shell
$ kubectl label nodes <NODE_1> team=blue
$ kubectl label nodes <NODE_2> team=red
$ kubectl label nodes <NODE_3> team=blue
```

Check, if labels have been applied:

```shell
$ kubectl get nodes --show-labels
```

Now, we are goin to deploy a pod with node affinity:

```yaml
apiVersion: v1
kind: Pod
metadata:
  name: mypod-node-affinity
spec:
  affinity:
    nodeAffinity:
      requiredDuringSchedulingIgnoredDuringExecution:
        nodeSelectorTerms:
        - matchExpressions:
          - key: team
            operator: In
            values:
            - blue
      preferredDuringSchedulingIgnoredDuringExecution:
      - weight: 1
        preference:
          matchExpressions:
          - key: failure-domain.beta.kubernetes.io/zone
            operator: In
            values:
            - 1
  containers:
  - name: mypod-node-affinity
    image: k8s.gcr.io/pause:2.0
```

The rules above say, that the pod will be scheduled on nodes with the label "team=blue" (required rule) and preferably on a node which is in the "failure-domain 1" (preffered rule).

Kubernetes supports the following operators when defining rules:

- In
- NotIn
- Exists
- DoesNotExist
- Gt
- Lt

> Anti-Affinity can be achieved by using `NotIn` and `DoesNotExist`.

### Pod Affinity / Anti-Affinity ###



#### Basic sample ####

As a sample for anti-affinity, imagine an application that needs a Redis cache in the cluster. We want to guarantee, that the replicas of the Redis cluster won't be scheduled on the same node.

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: redis-cache
spec:
  selector:
    matchLabels:
      app: cache
  replicas: 3
  template:
    metadata:
      labels:
        app: cache
    spec:
      affinity:
        podAntiAffinity:
          requiredDuringSchedulingIgnoredDuringExecution:
          - labelSelector:
              matchExpressions:
              - key: app
                operator: In
                values:
                - cache
            topologyKey: "kubernetes.io/hostname"
      containers:
      - name: redis-server
        image: redis:3.2-alpine
```

### Pod Priority ### 

..

## Resource Limits ##

## Horizontal Scaling ?? ##

## Best Practices ##

RunAsNonRoot

