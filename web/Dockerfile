# The website image (Next.js "standalone": only the files the server needs)
FROM node:22-alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
# Baked into the static pages (robots.txt, sitemap address, Google indexing on/off)
ARG SITE_URL=https://bestiapp.no
ARG ALLOW_INDEXING=false
# Visit statistics (Umami); empty = off
ARG UMAMI_WEBSITE_ID=
ENV SITE_URL=$SITE_URL ALLOW_INDEXING=$ALLOW_INDEXING UMAMI_WEBSITE_ID=$UMAMI_WEBSITE_ID NEXT_TELEMETRY_DISABLED=1
RUN npm run build

FROM node:22-alpine
WORKDIR /app
ARG SITE_URL=https://bestiapp.no
ARG ALLOW_INDEXING=false
ARG UMAMI_WEBSITE_ID=
ENV NODE_ENV=production NEXT_TELEMETRY_DISABLED=1 PORT=3000 HOSTNAME=0.0.0.0 \
    SITE_URL=$SITE_URL ALLOW_INDEXING=$ALLOW_INDEXING UMAMI_WEBSITE_ID=$UMAMI_WEBSITE_ID
COPY --from=build --chown=node:node /app/.next/standalone ./
COPY --from=build --chown=node:node /app/.next/static ./.next/static
USER node
EXPOSE 3000
CMD ["node", "server.js"]
