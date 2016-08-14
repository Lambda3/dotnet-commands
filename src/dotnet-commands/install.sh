#!/bin/bash
releases=$(curl https://github.com/Lambda3/dotnet-commands/releases.atom)
uri="https://github.com/Lambda3/dotnet-commands/releases/download/$(echo $releases | grep -oPm1 "(?<=<title>)[^<]+" | sed -n 2p)/dotnet-commands.tar.gz"
outFile=`tempfile`
curl -o $outFile -L $uri
outDir="$outFile-extracted"
echo $outDir
mkdir $outDir
tar -xvzf $outFile -C $outDir
echo $outDir
. "$outDir/dotnet-commands" install dotnet-commands --verbose --pre
if [ $? -ne 0 ]; then
    echo "Could not install."
    exit 1
fi
if [[ $PATH != *"$HOME/.nuget/commands/bin"* ]]; then
    echo "PATH=$HOME/.nuget/commands/bin:$PATH" >> $HOME/.bashrc
    echo "Your .bashrc was updated, either start a new bash instance from scratch or \`source ~/.bashrc\`."
fi