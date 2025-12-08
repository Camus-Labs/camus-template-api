# Dapr Commands

## ✅ Correct way - Running with fixed ports:
```bash
cd src/Adapters/emc.adapterdapr.components
dapr run --app-id camus-app --dapr-http-port 3500 --dapr-grpc-port 50001 --resources-path .
```

## Alternative - Running from project root with fixed ports:
```bash
dapr run --app-id camus-app --dapr-http-port 3500 --dapr-grpc-port 50001 --resources-path ./src/Adapters/emc.adapterdapr.components
```

## To test the secret store:
```bash
# Test if Dapr is working (run this in another terminal)
curl http://localhost:3500/v1.0/secrets/localsecretstore/queryEngineAccessSecret
```

## Note: 
- `--dapr-http-port 3500` ensures Dapr uses port 3500 (matches your app configuration)
- `--dapr-grpc-port 50001` sets a consistent gRPC port
- Without these flags, Dapr uses random ports which won't match your app
- Make sure you docker desktop is up and running all Dapr containers