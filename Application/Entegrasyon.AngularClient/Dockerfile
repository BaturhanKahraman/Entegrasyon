FROM node as build
WORKDIR /app
COPY . .

RUN npm ci && npm run build

FROM nginx:alpine
COPY --from=build /app/dist/entegrasyon-angular /usr/share/nginx/html/
EXPOSE 80
