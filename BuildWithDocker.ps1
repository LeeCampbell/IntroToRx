$IMG=docker build . -q
if (-not (Test-Path -Path build/output)) {
    New-Item -Type Directory build/output
}
$Output=resolve-path ./build/output
docker run --rm -v ${Output}:/output $IMG