FROM node:22-alpine

WORKDIR /usr/src/app

COPY package*.json ./

# Install dependencies with clean npm cache
RUN npm cache clean --force && npm install

# Copy application code
COPY . .

# Generate Prisma client
RUN npx prisma generate --schema=./prisma/schema.prisma || echo "No Prisma schema found, skipping"

# Build the application
RUN npm run build

EXPOSE 3000

# Use a healthcheck to ensure the app is running
HEALTHCHECK --interval=30s --timeout=30s --start-period=5s --retries=3 \
  CMD node -e "require('http').get('http://localhost:3000', res => res.statusCode === 200 ? process.exit(0) : process.exit(1))" || exit 1

CMD ["npm", "run", "start:dev"]
