# Server Setup & Deployment Guide

This comprehensive guide covers deploying Unity-MCP Server in various environments, from local development to production cloud deployments. Choose the setup that best fits your needs.

## 🎯 Deployment Overview

Unity-MCP Server can be deployed in multiple ways:

1. **[Local Development](#local-development)** - Quick setup for testing and development
2. **[Docker Deployment](#docker-deployment)** - Containerized deployment (recommended)
3. **[Cloud Deployment](#cloud-deployment)** - Scalable production hosting
4. **[Enterprise Setup](#enterprise-setup)** - High-availability and security-focused
5. **[CI/CD Integration](#cicd-integration)** - Automated deployment pipelines

## 🏠 Local Development

### Quick Local Setup

Perfect for development and testing:

```bash
# Option 1: .NET Global Tool
dotnet tool install -g com.IvanMurzak.Unity.MCP.Server
unity-mcp-server

# Option 2: Docker (recommended)
docker run -p 8080:8080 ivanmurzakdev/unity-mcp-server:latest

# Option 3: Build from source
git clone https://github.com/IvanMurzak/Unity-MCP.git
cd Unity-MCP/Unity-MCP-Server
dotnet run
```

### Development Configuration

Create `development.env` file:
```bash
UNITY_MCP_PORT=8080
UNITY_MCP_CLIENT_TRANSPORT=http
UNITY_MCP_LOG_LEVEL=debug
UNITY_MCP_PLUGIN_TIMEOUT=30000
UNITY_MCP_MAX_CONNECTIONS=5
UNITY_MCP_ENABLE_CORS=true
```

Load configuration:
```bash
# Using environment file
source development.env
unity-mcp-server

# Or with Docker
docker run --env-file development.env -p 8080:8080 ivanmurzakdev/unity-mcp-server:latest
```

## 🐳 Docker Deployment

### Basic Docker Setup

#### Single Container Deployment
```bash
# Simple deployment
docker run -d \
  --name unity-mcp-server \
  -p 8080:8080 \
  --restart unless-stopped \
  ivanmurzakdev/unity-mcp-server:latest

# With custom configuration
docker run -d \
  --name unity-mcp-server \
  -p 8080:8080 \
  -e UNITY_MCP_LOG_LEVEL=info \
  -e UNITY_MCP_MAX_CONNECTIONS=10 \
  -v $(pwd)/logs:/app/logs \
  --restart unless-stopped \
  ivanmurzakdev/unity-mcp-server:latest
```

#### Health Check Setup
```bash
docker run -d \
  --name unity-mcp-server \
  -p 8080:8080 \
  --health-cmd="curl -f http://localhost:8080/health || exit 1" \
  --health-interval=30s \
  --health-timeout=10s \
  --health-retries=3 \
  --restart unless-stopped \
  ivanmurzakdev/unity-mcp-server:latest
```

### Docker Compose Deployment

#### Basic Setup
Create `docker-compose.yml`:
```yaml
version: '3.8'

services:
  unity-mcp-server:
    image: ivanmurzakdev/unity-mcp-server:latest
    container_name: unity-mcp-server
    ports:
      - "8080:8080"
    environment:
      UNITY_MCP_PORT: 8080
      UNITY_MCP_CLIENT_TRANSPORT: http
      UNITY_MCP_LOG_LEVEL: info
      UNITY_MCP_MAX_CONNECTIONS: 10
    volumes:
      - ./logs:/app/logs
      - ./config:/app/config:ro
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s

volumes:
  unity_mcp_logs:
    driver: local
```

Deploy:
```bash
docker-compose up -d
```

#### Advanced Docker Compose Setup
```yaml
version: '3.8'

services:
  unity-mcp-server:
    image: ivanmurzakdev/unity-mcp-server:latest
    container_name: unity-mcp-server
    ports:
      - "8080:8080"
    environment:
      UNITY_MCP_PORT: 8080
      UNITY_MCP_CLIENT_TRANSPORT: http
      UNITY_MCP_LOG_LEVEL: info
    volumes:
      - unity_mcp_logs:/app/logs
      - ./config:/app/config:ro
    networks:
      - unity-mcp-network
    depends_on:
      - redis
      - prometheus
    restart: unless-stopped
    deploy:
      resources:
        limits:
          memory: 512M
          cpus: '0.5'
        reservations:
          memory: 256M
          cpus: '0.25'
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3

  redis:
    image: redis:alpine
    container_name: unity-mcp-redis
    ports:
      - "6379:6379"
    volumes:
      - redis_data:/data
    networks:
      - unity-mcp-network
    restart: unless-stopped

  prometheus:
    image: prom/prometheus
    container_name: unity-mcp-prometheus
    ports:
      - "9090:9090"
    volumes:
      - ./prometheus.yml:/etc/prometheus/prometheus.yml:ro
      - prometheus_data:/prometheus
    networks:
      - unity-mcp-network
    restart: unless-stopped

  grafana:
    image: grafana/grafana
    container_name: unity-mcp-grafana
    ports:
      - "3000:3000"
    volumes:
      - grafana_data:/var/lib/grafana
    networks:
      - unity-mcp-network
    restart: unless-stopped

networks:
  unity-mcp-network:
    driver: bridge

volumes:
  unity_mcp_logs:
  redis_data:
  prometheus_data:
  grafana_data:
```

### Docker Swarm Deployment

For multi-node deployments:

```yaml
version: '3.8'

services:
  unity-mcp-server:
    image: ivanmurzakdev/unity-mcp-server:latest
    ports:
      - "8080:8080"
    environment:
      UNITY_MCP_PORT: 8080
      UNITY_MCP_CLIENT_TRANSPORT: http
    deploy:
      replicas: 3
      restart_policy:
        condition: on-failure
        delay: 5s
        max_attempts: 3
      resources:
        limits:
          memory: 512M
          cpus: '0.5'
        reservations:
          memory: 256M
    networks:
      - unity-mcp-overlay

  traefik:
    image: traefik:v2.8
    ports:
      - "80:80"
      - "8081:8080" # Traefik dashboard
    command:
      - --api.dashboard=true
      - --providers.docker.swarmMode=true
      - --entrypoints.web.address=:80
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock:ro
    networks:
      - unity-mcp-overlay
    deploy:
      placement:
        constraints:
          - node.role == manager

networks:
  unity-mcp-overlay:
    driver: overlay
```

Deploy to swarm:
```bash
docker stack deploy -c docker-compose-swarm.yml unity-mcp
```

## ☁️ Cloud Deployment

### AWS Deployment

#### ECS Fargate Setup
Create `task-definition.json`:
```json
{
  "family": "unity-mcp-server",
  "networkMode": "awsvpc",
  "requiresCompatibilities": ["FARGATE"],
  "cpu": "256",
  "memory": "512",
  "executionRoleArn": "arn:aws:iam::ACCOUNT:role/ecsTaskExecutionRole",
  "containerDefinitions": [
    {
      "name": "unity-mcp-server",
      "image": "ivanmurzakdev/unity-mcp-server:latest",
      "portMappings": [
        {
          "containerPort": 8080,
          "protocol": "tcp"
        }
      ],
      "environment": [
        {
          "name": "UNITY_MCP_PORT",
          "value": "8080"
        },
        {
          "name": "UNITY_MCP_LOG_LEVEL",
          "value": "info"
        }
      ],
      "logConfiguration": {
        "logDriver": "awslogs",
        "options": {
          "awslogs-group": "/ecs/unity-mcp-server",
          "awslogs-region": "us-east-1",
          "awslogs-stream-prefix": "ecs"
        }
      },
      "healthCheck": {
        "command": [
          "CMD-SHELL",
          "curl -f http://localhost:8080/health || exit 1"
        ],
        "interval": 30,
        "timeout": 5,
        "retries": 3
      }
    }
  ]
}
```

Deploy with AWS CLI:
```bash
# Register task definition
aws ecs register-task-definition --cli-input-json file://task-definition.json

# Create service
aws ecs create-service \
  --cluster unity-mcp-cluster \
  --service-name unity-mcp-server \
  --task-definition unity-mcp-server:1 \
  --desired-count 2 \
  --launch-type FARGATE \
  --network-configuration "awsvpcConfiguration={subnets=[subnet-12345],securityGroups=[sg-12345],assignPublicIp=ENABLED}"
```

#### EKS (Kubernetes) Setup
Create `kubernetes-deployment.yaml`:
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: unity-mcp-server
  labels:
    app: unity-mcp-server
spec:
  replicas: 3
  selector:
    matchLabels:
      app: unity-mcp-server
  template:
    metadata:
      labels:
        app: unity-mcp-server
    spec:
      containers:
      - name: unity-mcp-server
        image: ivanmurzakdev/unity-mcp-server:latest
        ports:
        - containerPort: 8080
        env:
        - name: UNITY_MCP_PORT
          value: "8080"
        - name: UNITY_MCP_LOG_LEVEL
          value: "info"
        resources:
          requests:
            memory: "256Mi"
            cpu: "250m"
          limits:
            memory: "512Mi"
            cpu: "500m"
        livenessProbe:
          httpGet:
            path: /health
            port: 8080
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /health
            port: 8080
          initialDelaySeconds: 5
          periodSeconds: 5

---
apiVersion: v1
kind: Service
metadata:
  name: unity-mcp-service
spec:
  selector:
    app: unity-mcp-server
  ports:
    - protocol: TCP
      port: 80
      targetPort: 8080
  type: LoadBalancer

---
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: unity-mcp-ingress
  annotations:
    kubernetes.io/ingress.class: "nginx"
    cert-manager.io/cluster-issuer: "letsencrypt-prod"
spec:
  tls:
  - hosts:
    - unity-mcp.yourdomain.com
    secretName: unity-mcp-tls
  rules:
  - host: unity-mcp.yourdomain.com
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: unity-mcp-service
            port:
              number: 80
```

Deploy:
```bash
kubectl apply -f kubernetes-deployment.yaml
```

### Google Cloud Platform (GCP)

#### Cloud Run Deployment
```bash
# Deploy to Cloud Run
gcloud run deploy unity-mcp-server \
  --image=ivanmurzakdev/unity-mcp-server:latest \
  --platform=managed \
  --port=8080 \
  --allow-unauthenticated \
  --set-env-vars="UNITY_MCP_LOG_LEVEL=info" \
  --memory=512Mi \
  --cpu=1 \
  --max-instances=10 \
  --region=us-central1
```

#### GKE Autopilot Setup
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: unity-mcp-server
spec:
  replicas: 2
  selector:
    matchLabels:
      app: unity-mcp-server
  template:
    metadata:
      labels:
        app: unity-mcp-server
    spec:
      containers:
      - name: unity-mcp-server
        image: ivanmurzakdev/unity-mcp-server:latest
        ports:
        - containerPort: 8080
        env:
        - name: UNITY_MCP_PORT
          value: "8080"
        resources:
          requests:
            memory: "256Mi"
            cpu: "250m"
          limits:
            memory: "512Mi"
            cpu: "500m"

---
apiVersion: v1
kind: Service
metadata:
  name: unity-mcp-service
  annotations:
    cloud.google.com/neg: '{"ingress": true}'
spec:
  type: ClusterIP
  selector:
    app: unity-mcp-server
  ports:
  - port: 80
    targetPort: 8080
```

### Azure Deployment

#### Container Instances
```bash
# Deploy to Azure Container Instances
az container create \
  --resource-group unity-mcp-rg \
  --name unity-mcp-server \
  --image ivanmurzakdev/unity-mcp-server:latest \
  --ports 8080 \
  --ip-address public \
  --environment-variables UNITY_MCP_LOG_LEVEL=info \
  --memory 0.5 \
  --cpu 0.5
```

#### AKS Deployment
Similar to EKS, but with Azure-specific ingress:
```yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: unity-mcp-ingress
  annotations:
    kubernetes.io/ingress.class: azure/application-gateway
spec:
  rules:
  - host: unity-mcp.yourdomain.com
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: unity-mcp-service
            port:
              number: 80
```

## 🏢 Enterprise Setup

### High Availability Configuration

#### Load Balancer Setup (nginx)
Create `nginx.conf`:
```nginx
upstream unity_mcp_backend {
    least_conn;
    server 127.0.0.1:8080 max_fails=3 fail_timeout=30s;
    server 127.0.0.1:8081 max_fails=3 fail_timeout=30s;
    server 127.0.0.1:8082 max_fails=3 fail_timeout=30s;
}

server {
    listen 80;
    server_name unity-mcp.company.com;
    
    location / {
        proxy_pass http://unity_mcp_backend;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        
        # Health check
        proxy_next_upstream error timeout invalid_header http_500 http_502 http_503 http_504;
        proxy_connect_timeout 5s;
        proxy_send_timeout 10s;
        proxy_read_timeout 30s;
    }
    
    location /health {
        access_log off;
        proxy_pass http://unity_mcp_backend;
    }
}
```

#### SSL/TLS Configuration
```nginx
server {
    listen 443 ssl http2;
    server_name unity-mcp.company.com;
    
    ssl_certificate /etc/ssl/certs/unity-mcp.crt;
    ssl_certificate_key /etc/ssl/private/unity-mcp.key;
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers ECDHE-RSA-AES128-GCM-SHA256:ECDHE-RSA-AES256-GCM-SHA384;
    ssl_prefer_server_ciphers off;
    
    location / {
        proxy_pass http://unity_mcp_backend;
        # ... proxy settings
    }
}
```

### Security Hardening

#### Firewall Configuration
```bash
# Ubuntu/Debian
sudo ufw allow 22/tcp
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
sudo ufw deny 8080/tcp  # Block direct access
sudo ufw enable

# CentOS/RHEL
firewall-cmd --permanent --add-service=ssh
firewall-cmd --permanent --add-service=http
firewall-cmd --permanent --add-service=https
firewall-cmd --permanent --remove-port=8080/tcp
firewall-cmd --reload
```

#### Container Security
```yaml
version: '3.8'

services:
  unity-mcp-server:
    image: ivanmurzakdev/unity-mcp-server:latest
    user: "1000:1000"  # Non-root user
    read_only: true
    tmpfs:
      - /tmp:noexec,nosuid,size=100m
    cap_drop:
      - ALL
    cap_add:
      - NET_BIND_SERVICE
    security_opt:
      - no-new-privileges:true
    environment:
      UNITY_MCP_PORT: 8080
    volumes:
      - ./logs:/app/logs
    networks:
      - unity-mcp-internal
    restart: unless-stopped

networks:
  unity-mcp-internal:
    driver: bridge
    internal: true
```

## 🔄 CI/CD Integration

### GitHub Actions Deployment

Create `.github/workflows/deploy.yml`:
```yaml
name: Deploy Unity-MCP Server

on:
  push:
    branches: [main]
  release:
    types: [published]

jobs:
  deploy:
    runs-on: ubuntu-latest
    
    steps:
    - name: Checkout code
      uses: actions/checkout@v3
    
    - name: Configure AWS credentials
      uses: aws-actions/configure-aws-credentials@v2
      with:
        aws-access-key-id: ${{ secrets.AWS_ACCESS_KEY_ID }}
        aws-secret-access-key: ${{ secrets.AWS_SECRET_ACCESS_KEY }}
        aws-region: us-east-1
    
    - name: Login to Amazon ECR
      uses: aws-actions/amazon-ecr-login@v1
    
    - name: Build and push Docker image
      env:
        ECR_REGISTRY: ${{ steps.login-ecr.outputs.registry }}
        ECR_REPOSITORY: unity-mcp-server
        IMAGE_TAG: ${{ github.sha }}
      run: |
        docker build -t $ECR_REGISTRY/$ECR_REPOSITORY:$IMAGE_TAG .
        docker push $ECR_REGISTRY/$ECR_REPOSITORY:$IMAGE_TAG
    
    - name: Deploy to ECS
      env:
        CLUSTER_NAME: unity-mcp-cluster
        SERVICE_NAME: unity-mcp-server
        IMAGE_URI: ${{ steps.login-ecr.outputs.registry }}/unity-mcp-server:${{ github.sha }}
      run: |
        aws ecs update-service \
          --cluster $CLUSTER_NAME \
          --service $SERVICE_NAME \
          --force-new-deployment \
          --task-definition unity-mcp-server
```

### GitLab CI/CD

Create `.gitlab-ci.yml`:
```yaml
stages:
  - build
  - test
  - deploy

variables:
  DOCKER_DRIVER: overlay2
  DOCKER_TLS_CERTDIR: ""

build:
  stage: build
  image: docker:latest
  services:
    - docker:dind
  script:
    - docker build -t $CI_REGISTRY_IMAGE:$CI_COMMIT_SHA .
    - docker push $CI_REGISTRY_IMAGE:$CI_COMMIT_SHA
  only:
    - main

deploy_staging:
  stage: deploy
  image: alpine/helm:latest
  script:
    - helm upgrade --install unity-mcp-staging ./helm-chart \
        --set image.tag=$CI_COMMIT_SHA \
        --set ingress.host=staging.unity-mcp.company.com
  environment:
    name: staging
    url: https://staging.unity-mcp.company.com
  only:
    - main

deploy_production:
  stage: deploy
  image: alpine/helm:latest
  script:
    - helm upgrade --install unity-mcp-prod ./helm-chart \
        --set image.tag=$CI_COMMIT_SHA \
        --set ingress.host=unity-mcp.company.com \
        --set resources.limits.memory=1Gi
  environment:
    name: production
    url: https://unity-mcp.company.com
  when: manual
  only:
    - main
```

## 📊 Monitoring & Observability

### Prometheus Configuration

Create `prometheus.yml`:
```yaml
global:
  scrape_interval: 15s

scrape_configs:
  - job_name: 'unity-mcp-server'
    static_configs:
      - targets: ['unity-mcp-server:8080']
    metrics_path: /metrics
    scrape_interval: 10s

  - job_name: 'node-exporter'
    static_configs:
      - targets: ['node-exporter:9100']

rule_files:
  - "unity-mcp-rules.yml"

alerting:
  alertmanagers:
    - static_configs:
        - targets:
          - alertmanager:9093
```

### Grafana Dashboard
```json
{
  "dashboard": {
    "title": "Unity-MCP Server Monitoring",
    "panels": [
      {
        "title": "Request Rate",
        "type": "graph",
        "targets": [
          {
            "expr": "rate(unity_mcp_requests_total[5m])",
            "legendFormat": "Requests/sec"
          }
        ]
      },
      {
        "title": "Response Time",
        "type": "graph",
        "targets": [
          {
            "expr": "histogram_quantile(0.95, unity_mcp_request_duration_seconds_bucket)",
            "legendFormat": "95th percentile"
          }
        ]
      }
    ]
  }
}
```

## 🔧 Maintenance & Updates

### Automated Updates
```bash
#!/bin/bash
# update-unity-mcp.sh

# Pull latest image
docker pull ivanmurzakdev/unity-mcp-server:latest

# Graceful update with zero downtime
docker-compose pull
docker-compose up -d --no-deps unity-mcp-server

# Cleanup old images
docker image prune -f
```

### Backup Strategy
```bash
#!/bin/bash
# backup-unity-mcp.sh

# Backup configuration
tar -czf "unity-mcp-backup-$(date +%Y%m%d).tar.gz" \
  docker-compose.yml \
  config/ \
  logs/

# Upload to S3
aws s3 cp "unity-mcp-backup-$(date +%Y%m%d).tar.gz" \
  s3://unity-mcp-backups/

# Cleanup old backups (keep last 30 days)
find . -name "unity-mcp-backup-*.tar.gz" -mtime +30 -delete
```

### Health Monitoring
```bash
#!/bin/bash
# health-check.sh

HEALTH_URL="http://localhost:8080/health"
WEBHOOK_URL="https://hooks.slack.com/services/YOUR/SLACK/WEBHOOK"

if ! curl -f $HEALTH_URL > /dev/null 2>&1; then
  curl -X POST -H 'Content-type: application/json' \
    --data '{"text":"🚨 Unity-MCP Server is DOWN!"}' \
    $WEBHOOK_URL
fi
```

## 📚 Next Steps

### Advanced Configuration
- **[Configuration Guide](Configuration)** - Detailed configuration options
- **[Troubleshooting](Troubleshooting)** - Deployment problem solving
- **[API Reference](API-Reference)** - Technical server documentation

### Development Integration
- **[Custom Tools Development](Custom-Tools-Development)** - Extend server functionality
- **[Examples & Tutorials](Examples-and-Tutorials)** - Practical deployment examples

### Operations
- Set up monitoring and alerting
- Implement backup and disaster recovery
- Plan capacity scaling strategies
- Document deployment procedures

---

**Ready to deploy Unity-MCP Server?** Start with the [Local Development](#local-development) setup, then move to [Docker Deployment](#docker-deployment) for production use. Need help troubleshooting? Check our [Troubleshooting guide](Troubleshooting)!