Set-Location $PsScriptRoot
$img = docker image inspect hostingdemo:latest | convertfrom-json
if($args.Contains("--build")  || $img.Length -eq 0){
  docker build --force-rm -t hostingdemo -f Dockerfile ..
}

#$STACK_NAME='hydra-host'
mkdir -Force -p ./data/logs 
mkdir -Force -p ./data/redis 
if (Test-Path -Path ./data/logs/hls.log) {
  Remove-Item -Force ./data/logs/hls.log
}
docker compose up -d
#docker stack deploy --compose-file docker-compose.yml $STACK_NAME
# Start-Sleep 10
# docker stack deploy --compose-file stack-compose.yml --with-registry-auth $STACK_NAME
#Write-Output "HydraRouter available at: http://localhost:5353"
Write-output "Started"