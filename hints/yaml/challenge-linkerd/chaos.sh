#!/bin/bash

while true; do
  kubectl \
    --namespace challengelinkerd \
    -o 'jsonpath={.items[*].metadata.name}' \
    get pods --selector app=backend  | \
      tr " " "\n" | \
      shuf | \
      head -n 1 |
      xargs -t \
        kubectl --namespace challengelinkerd delete pod
  sleep 45
done