FROM node:22-alpine

WORKDIR /usr/src/app

COPY package*.json ./

# Install dependencies with clean npm cache
RUN npm cache clean --force && npm install

# Copy application code
COPY . .

# Generate .env from .env.example with Docker-appropriate values
RUN cp .env.example .env && \
    sed -i 's/DB_HOST=localhost/DB_HOST=db/g' .env && \
    sed -i 's/DB_PORT=3306/DB_PORT=3306/g' .env && \
    sed -i 's/DATABASE_URL=.*/DATABASE_URL="mysql:\/\/root:@db:3306\/nsdocs_consumption"/g' .env && \
    cat .env

RUN npm run build

EXPOSE 3000

# Use a healthcheck to ensure the app is running
HEALTHCHECK --interval=30s --timeout=30s --start-period=5s --retries=3 \
  CMD node -e "require('http').get('http://localhost:3000', res => res.statusCode === 200 ? process.exit(0) : process.exit(1))" || exit 1

# Start the application
CMD ["sh", "-c", "npx prisma db push --accept-data-loss && npm run seed && npm run start:prod"]
