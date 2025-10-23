# Build stage
FROM node:18-alpine AS build

WORKDIR /app

# Install dependencies first
RUN apk add --no-cache python3 make g++

# Copy package files
COPY package*.json ./

# Clear npm cache and install dependencies
RUN npm cache clean --force
RUN npm ci

# Copy source code
COPY . .

# Build da aplicação
RUN npm run build

# Production stage
FROM nginx:alpine

# Copiar configuração customizada do nginx
COPY nginx.conf /etc/nginx/nginx.conf

# Copiar arquivos buildados do Angular 18+ (browser directory)
COPY --from=build /app/dist/frontend/browser /usr/share/nginx/html

# Expor porta
EXPOSE 80

# Comando para iniciar nginx
CMD ["nginx", "-g", "daemon off;"]
