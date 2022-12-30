# Message Demo App



---

## RAW Redis CLI comands
Using the Docker Desktop dashboard, open a terminal to the Redis container connect to the Redis CLI.

```shell
redis-cli
```

### List PubSub channels
```
pubsub channels
pubsub channels *sender*
```

### Subscribe to a channel
```
subscribe hydra:service:mc:sender-svcs, hydra:service:mc:sender-svcs:85d02978c67c4f27aa12f39bdb5ea53d
```

### Send a message to a channel
```json
{
    "to":"sender-svcs:/",
    "frm": "external-client:/",
    "typ": "command",
    "bdy": {
        "cmd": "start"
    }
}
```

Above message has to be sent as an escaped JSON string

```
"hydra:service:mc:sender-svcs:85d02978c67c4f27aa12f39bdb5ea53d"
3) "{\"to\":\"85d02978c67c4f27aa12f39bdb5ea53d@sender-svcs:/\",\"frm\":\"external-client:/\",\"mid\":\"fe60ba9b-84ed-43be-90cd-a5f6eaa43f87\",\"ts\":\"2022-12-30T23:17:00.601Z\",\"typ\":\"command\",\"ver\":\"UMF/1.4.6\",\"via\":\"37528e822e70454cae2a2aa33643d791-1f7ljdkwrkz@hydra-router:/\",\"bdy\":{\"cmd\":\"start\"}}"
```




