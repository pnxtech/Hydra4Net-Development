$REDIS_ID=$(docker ps -aqf "name=redis")
docker exec -it $REDIS_ID /usr/bin/redis-cli -n 0 save > $null
Start-Sleep 5
docker compose down
