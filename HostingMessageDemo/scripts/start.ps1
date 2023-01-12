# Set-Location $PsScriptRoot
$img = docker image inspect hostingdemo:latest | convertfrom-json
if($args.Contains("--build")  || $img.Length -eq 0){
  docker build --force-rm -t hostingdemo -f ../Dockerfile "$PsScriptRoot/../.."
}

$dataFolder = "$PsScriptRoot/../data"

$null = mkdir -Force -p $dataFolder/logs 
$null = mkdir -Force -p $dataFolder/redis 
if (Test-Path -Path $dataFolder/logs/hls.log) {
  Remove-Item -Force $dataFolder/logs/hls.log
}

docker compose -f "$PsScriptRoot/docker-compose.yml" up -d
Write-output "Started"