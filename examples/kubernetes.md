# Kubernetes examples

The following examples show how to setup the bot application using Kubernetes. As discussed in the README, configuration can be done in a variety of ways. These examples will use environment variables for configuration, but feel free to use `appsettings.yaml`, command line arguments, other file types or the config directory.

## Bot application

This example shows how to run the bot application using Kubernetes. This is just a simple example, using basic resources. Feel free to template and deploy to Kubernetes as you see fit.

_kubernetes.yaml_
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: server-manager-discord-bot
  namespace: server-manager-discord-bot
spec:
  selector:
    matchLabels:
      app: server-manager-discord-bot
  template:
    metadata:
      labels:
        app: server-manager-discord-bot
    spec:
      serviceAccountName: server-manager-discord-bot
      containers:
        - name: server-manager-discord-bot
          image: ghcr.io/cruikshj/server-manager-discord-bot
          env:
            - name: SERVERMANAGER_BotToken
              value: "<bottoken>"
            - name: SERVERMANAGER_ServerInfoProviders__0__Type
              value: "KubernetesConfigMap"
---
apiVersion: v1
kind: ServiceAccount
metadata:
  name: server-manager-discord-bot
---
kind: ClusterRole
apiVersion: rbac.authorization.k8s.io/v1
metadata:
  name: server-manager-discord-bot
rules:
- apiGroups:
  - ""
  resources:
  - configmaps
  verbs:
  - get
  - list
---
kind: ClusterRoleBinding
apiVersion: rbac.authorization.k8s.io/v1
metadata:
  name: server-manager-discord-bot
roleRef:
  apiGroup: rbac.authorization.k8s.io
  kind: ClusterRole
  name: server-manager-discord-bot
subjects:
- kind: ServiceAccount
  name: server-manager-discord-bot
  namespace: server-manager-discord-bot
---
apiVersion: v1
kind: ConfigMap
metadata:
  name: my-servers-config
  namespace: whereever
  labels:
    server-manager: default
data:
  "minecraft-1": |
    Game: Minecraft (Bedrock)
    Icon: https://cdn2.steamgriddb.com/icon_thumb/4a5b76e7170df685ed8b75c7dacce268.png
    Fields:
      Address: example.com:12345
      Mode: Survival
  "minecraft-2": |
    Game: Minecraft (Java)
    Icon: https://cdn2.steamgriddb.com/icon_thumb/4a5b76e7170df685ed8b75c7dacce268.png
    Fields:
      Address: example.com:54321
      Mode: Creative
      Version: 1.12.2
    Readme: |
      # Minecraft
      This is a Minecraft server.
```

## Kubernetes integration

This example expands on the above example to show how the bot can manage dedicated servers using Kubernetes.

_kubernetes.yaml_
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: server-manager-discord-bot
  namespace: server-manager-discord-bot
spec:
  selector:
    matchLabels:
      app: server-manager-discord-bot
  template:
    metadata:
      labels:
        app: server-manager-discord-bot
    spec:
      serviceAccountName: server-manager-discord-bot
      containers:
        - name: server-manager-discord-bot
          image: ghcr.io/cruikshj/server-manager-discord-bot
          env:
            - name: SERVERMANAGER_BotToken
              value: "<bottoken>"
            - name: SERVERMANAGER_ServerInfoProviders__0__Type
              value: "KubernetesConfigMap"
            - name: SERVERMANAGER_ServerHostAdapters__Kubernetes__Type
              value: "Kubernetes"
---
apiVersion: v1
kind: ServiceAccount
metadata:
  name: server-manager-discord-bot
---
kind: ClusterRole
apiVersion: rbac.authorization.k8s.io/v1
metadata:
  name: server-manager-discord-bot
rules:
rules:
- apiGroups:
  - ""
  resources:
  - configmaps
  verbs:
  - get
  - list
- apiGroups:
  - ""
  resources:
  - pods
  - pods/log
  verbs:
  - get
  - list
- apiGroups:
  - apps
  resources:
  - deployments
  - deployments/status
  verbs:
  - get
- apiGroups:
  - apps
  resources:
  - deployments/scale
  verbs:
  - get
  - patch
---
kind: ClusterRoleBinding
apiVersion: rbac.authorization.k8s.io/v1
metadata:
  name: server-manager-discord-bot
roleRef:
  apiGroup: rbac.authorization.k8s.io
  kind: ClusterRole
  name: server-manager-discord-bot
subjects:
- kind: ServiceAccount
  name: server-manager-discord-bot
  namespace: server-manager-discord-bot
---
apiVersion: v1
kind: ConfigMap
metadata:
  name: my-servers-config
  namespace: whereever
  labels:
    server-manager: default
data:
  "minecraft-1": |
    Game: Minecraft (Bedrock)
    Icon: https://cdn2.steamgriddb.com/icon_thumb/4a5b76e7170df685ed8b75c7dacce268.png
    Fields:
      Address: example.com:12345
      Mode: Survival
    HostAdapter: Kubernetes
    HostProperties:
      Kind: Deployment
      Namespace: minecraft-1
      Name: minecraft-1
  "minecraft-2": |
    Game: Minecraft (Java)
    Icon: https://cdn2.steamgriddb.com/icon_thumb/4a5b76e7170df685ed8b75c7dacce268.png
    Fields:
      Address: example.com:54321
      Mode: Creative
      Version: 1.12.2
    Readme: |
      # Minecraft
      This is a Minecraft server.
    HostAdapter: Kubernetes
    HostProperties:
      Kind: Deployment
      Namespace: minecraft-2
      Name: minecraft-2
```