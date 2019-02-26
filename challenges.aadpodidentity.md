# Create a Kubernetes pod that uses Managed Service Identity (MSI) to access an Azure Key Vault

## Here is what you learn ##

- Create a [user-assigned managed identity](https://docs.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/overview) 
- Install [aad-pod-identity](https://github.com/Azure/aad-pod-identity) in your cluster
- Create an Azure Key Vault and store credentials
- Deploy a pod that uses a user-assigned managed identity to access an Azure Key Vault

## Deploy the  aad-pod-identity infra in your existing cluster ##

Open a shell and run the following ```kubectl create``` command:

```Shell
kubectl create -f https://raw.githubusercontent.com/Azure/aad-pod-identity/1.3.0-mic-1.4.0-nmi/deploy/infra/deployment.yaml
```

If you have an RBAC enabled cluster run the following ````kubectl create``` command:

```Shell
kubectl create -f https://raw.githubusercontent.com/Azure/aad-pod-identity/1.3.0-mic-1.4.0-nmi/deploy/infra/deployment-rbac.yaml
```

Now you have NMI and MIC running in your cluster. To get a detailed description of NMI and MIC take a look [here](https://github.com/Azure/aad-pod-identity#design)

## Create an Azure user-assigned managed identity ##

Remember the client id, principal id and resource id for the identity.

```Shell
az identity create -g <resource-groupname> -n <managedidentity-resourcename>
```

Now you have created a manaed identity with the name ```managedidentity-name```. Take a look at your ```resource-groupname```. The identity is listed as an Azure resource.

## Assign Reader Role to the new created identity ## 

Using the principal id from the last step, assign the reader role to the new created identity for the resource group.

```
az role assignment create --role Reader --assignee <principal-id> --scope /subscriptions/<subscriptionid>/resourcegroups/<resourcegroup>
```

## Get your AKS Service Principal object id ##

If you don't know the Service Principal that is used for your Cluster do the following:

```
az aks show -n <akscluster-name> -g <resource-groupname>
```

Rember the client id from the output under the section:

```JSON
 "servicePrincipalProfile": {
    "clientId": "<client id>"
  },
```

After that run the following command to get details of the Service Principal

```
az ad sp show --id <client id>
```

Remember the object id from the output.

```JSON
"objectId": "<object id>"
```

## Create permissions for MIC ##

MIC uses the service principal credentials stored within the the AKS cluster to access azure resources. This service principal needs to have Microsoft.ManagedIdentity/userAssignedIdentities/*/assign/action permission on the identity for usage with User assigned MSI.

Assign the required permission and use the object id from the previous step as sp id.

```Shell
az role assignment create --role "Managed Identity Operator" --assignee <sp id> --scope <full id of the managed identity>
```

## Create an Azure KeyVault

To demo AAD pod identity we create an Azure KeyVault and grant read access for the created user-assigned identity.

Create an Azure KeyVault in your resource group and remember the id from the output.

```Shell
>az keyvault create -n <global unique name> -g <resource group> --sku standard
```

Set access ploicies for your user-assigned identity using the principal id from the step above.

```Shell
>az keyvault set-policy -n <keyvault name> --object-id <principal id of your user-assigned identity> --secret-permissions get, list --key-permissions get, list --certificate-permissions get, list
```

Add some secrets to your key vault that we will be used later in the demo application.
Do not change the name of the secrets!

```Shell
>az keyvault secret set --vault-name <key vault name> --name Settings--ValueOne --value DemoValueOne
>az keyvault secret set --vault-name <key vault name> --name Settings--ValueTwo --value DemoValueTwo
```

## Install Identity on your AKS Cluster ##

Edit and save the file [aadpodidentity.yaml](/src/aadpodidentity/deployment/aadpodidentity.yaml).
Replace clientid and managedidentity-resourcename.

```YAML
apiVersion: "aadpodidentity.k8s.io/v1"
kind: AzureIdentity
metadata:
 name: demoidentity
spec:
 type: 0
 ResourceID: /subscriptions/<subid>/resourcegroups/<resourcegroup>/providers/Microsoft.ManagedIdentity/userAssignedIdentities/<managedidentity-resourcename>
 ClientID: <clientid>
```

```Shell
kubectl create -f aadpodidentity.yaml 
```

## Install Pod to Identity binding ##

Edit and save the file [aadpodidentitybinding.yaml](/src/aadpodidentity/deployment/aadpodidentitybinding.yaml)

```YAML
apiVersion: "aadpodidentity.k8s.io/v1"
kind: AzureIdentityBinding
metadata:
 name: demo1-azure-identity-binding
spec:
 AzureIdentity: demoidentity
 Selector: azureidentitydemo
```

```Shell
kubectl create -f aadpodidentitybinding.yaml
```

## Install demo application ##

```Shell
ToDo
```