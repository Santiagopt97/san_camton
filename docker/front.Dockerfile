# Imagen común de los 5 fronts. Construir desde la raíz del repositorio:
#   docker build -f docker/front.Dockerfile --build-arg FRONT_DIR=landing-front \
#     --build-arg "VITE_VARS=VITE_API_URL=http://localhost:5003 VITE_APP_URL=http://localhost:5173" -t hotel-landing-front:dev .
# VITE_VARS lleva solo las variables que se quieren fijar: una variable vacía anularía el valor por defecto del front.
FROM node:22-alpine AS build
ARG FRONT_DIR
ARG VITE_VARS=""
WORKDIR /src
COPY hotel-ui/package.json hotel-ui/package-lock.json* hotel-ui/
COPY ${FRONT_DIR}/package.json ${FRONT_DIR}/package-lock.json ${FRONT_DIR}/
COPY hotel-ui/ hotel-ui/
WORKDIR /src/${FRONT_DIR}
RUN npm ci
COPY ${FRONT_DIR}/ ./
RUN env ${VITE_VARS} npm run build

FROM nginxinc/nginx-unprivileged:alpine
ARG FRONT_DIR
COPY docker/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /src/${FRONT_DIR}/dist /usr/share/nginx/html
EXPOSE 8080
