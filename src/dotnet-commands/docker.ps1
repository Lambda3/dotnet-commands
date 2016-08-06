param(
    [Parameter(ParameterSetName='build', Mandatory=$true)][switch]$build,
    [Parameter(ParameterSetName='run', Mandatory=$true)][switch]$run
)
if ($build) {
    docker build --tag giggio/dotnetdev $PSScriptRoot
}
if ($run) {
    $id=$(docker ps -f name=dotnet-commands -q)
    if ($id) {
        echo "Already running, attaching..."
        docker attach $id
        exit
    }
    $id=$(docker ps -f name=dotnet-commands -a -q)
    if ($id) {
        echo "Already created, starting and attaching..."
        docker start --attach --interactive $id
    } else {
        echo "Creating..."
        docker run -ti --name dotnet-commands -v "${PSScriptRoot}\..\..\:/app" giggio/dotnetdev
    }
}