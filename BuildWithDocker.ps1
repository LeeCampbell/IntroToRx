$IMG=docker build . -q
New-Item -Type Directory build/output
$Output=resolve-path ./build/output
docker run --rm -v ${Output}:/output $IMG