# Message Demo App



## RAW Redis CLI comands
Using the Docker Desktop dashboard, open a terminal to the Redis container and connect to the Redis CLI from the terminal prompt.

```shell
redis-cli
```

or from windows terminal prompt (not the powershell) or Mac / Linux command shell:

Obtain the Redis docker container ID using `docker ps` and:

```sh
docker exec -it 5e596b786f4d redis-cli
```

### List PubSub channels
```
pubsub channels *sender*
```

### Subscribe to a channel
```
subscribe hydra:service:mc:sender-svcs, hydra:service:mc:sender-svcs:7f594b2f53004980911e02c5ce2e46f5
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

```

View queued items


```
lrange hydra:service:queuer-svcs:mqrecieved 0 99
```

