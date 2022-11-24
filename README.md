# Hydra4Net-Development

Hydra library for .NET

Hydra4Net is an .NET implementation of the ideas/methods for building distributed applications based on the original [Hydra](https://github.com/pnxtech/hydra) project - which leverge the use of the Redis realtime Database.

This project is a development environment for Hydra4Net. The TestRig project is a console application which utilizes the Hydra4Net library.


## Build container

```shell
 dotnet publish -c Release
 docker build -t testrig:1.0.0 -f .\TestRig\Dockerfile .
```

