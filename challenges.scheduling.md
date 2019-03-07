# Advanced Scheduling / Resource Management #

Kubernetes has some advanced concepts when it comes to resource handling and scheduling of your workloads. In this challenge, you will learn about how scheduling works in Kubernetes, how you can influence which nodes will be selected and how pods can be prioritized. 

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

"IgnoredDuringExecution" means that node labels being changed at runtime won't affect pods currently running on that specific node.

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

Pod Affinity / Pod Anti-Affinity work the same way as Node Affinity, except that scheduling is based on labels on pods that are already running on one node rather than node labels. The rules are of the form “this pod should (or, in the case of anti-affinity, should not) run on *node X* if that *node X* is already running one or more pods that meet rule *Y*”. 

*Node X* is determined by a `topologyKey` and *Y* is expressed as a label seletor.

> You can add a list of namespaces, the pod selectors will work on. By default, it will query only pods in the current namespace!

#### Basic sample for anti-affinity ####

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
        image: redis
```

### Pod Priority ### 

Priority indicates the importance of a pod relative to other pods. A pod priority influences the scheduling of a pod and out-of-resource eviction ordering on the node.

#### Sample ####

First, let's create two priority classes we can use when scheduling pods - one "high" and one "low"-prio class.

> Note: A PriorityClass is non-namespaced.

```yaml
apiVersion: scheduling.k8s.io/v1beta1
kind: PriorityClass
metadata:
  name: myhigh-priority
value: 100
globalDefault: false
description: "This is the high-prio class."
---
apiVersion: scheduling.k8s.io/v1beta1
kind: PriorityClass
metadata:
  name: mylow-priority
value: 10
globalDefault: false
description: "This is the high-prio class."
```

Now, we need to simulate a situation, where pods can't be scheduled anymore. Therefore, we deploy many pods, that alltogether request a lot of CPU.

```yaml
apiVersion: extensions/v1beta1
kind: Deployment
metadata:
  name: lowprio-pods
spec:
  replicas: 30
  template:
    metadata:
      labels:
        name: lowprio-pods
    spec:
      containers:
        - name: lowprio-pods
          image: k8s.gcr.io/pause:2.0
          resources:
            requests:
              cpu: "250m"
              memory: "64Mi"
            limits:
              memory: "128Mi"
              cpu: "2000m"
      priorityClassName: mylow-priority
```

You can see, that some of the pods can't be scheduled, because Kubernetes reports low resources.

Now, if we wanted to deploy further pods that definitely need to run, we wouldn't be able to do so - except: we can give them a higher priority which leads to evication of running pods with "low-priority".

You can test the behavior by deploying two pods with the "myhigh-priority"

```yaml
apiVersion: extensions/v1beta1
kind: Deployment
metadata:
  name: highprio-pods
spec:
  replicas: 2
  template:
    metadata:
      labels:
        name: highprio-pods
    spec:
      containers:
        - name: highprio-pods
          image: k8s.gcr.io/pause:2.0
          resources:
            requests:
              cpu: "1000m"
              memory: "64Mi"
            limits:
              memory: "128Mi"
              cpu: "2000m"
      priorityClassName: myhigh-priority
```

#### House-Keeping ####

Delete the deployments and priority classes.

```shell
$ kubectl delete -f .\lowprio-pods.yaml
$ kubectl delete -f .\highprio-pods.yaml
$ kubectl delete -f .\priority-classes.yaml
```

## Resource Limits ##



## Horizontal Scaling ?? ##

## Best Practices ##

RunAsNonRoot

