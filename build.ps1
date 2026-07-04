param ([string]$tag = "latest")

docker build . -f src\AccountService\Dockerfile -t vladimirkhil/accountservice:$tag
