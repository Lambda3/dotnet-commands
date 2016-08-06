FROM microsoft/dotnet:1.0.0-preview2-sdk
MAINTAINER giggio@giggio.net

RUN apt-get update
RUN apt-get install git vim -y

VOLUME [ "/app" ]
WORKDIR /app

ENTRYPOINT [ "/bin/bash" ]