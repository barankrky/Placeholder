FROM node:18-alpine

# Create app directory
WORKDIR /app

# Copy package.json and package-lock.json
COPY package*.json ./

# Install dependencies
RUN npm install

# Copy source code and TypeScript config
COPY tsconfig.json ./
COPY src/ ./src/

# Build the application
RUN npm run build

# Default environment variable
ENV MCP_WEBSOCKET_PORT=5010

# Expose the port dynamically using the environment variable
EXPOSE ${MCP_WEBSOCKET_PORT}

# Command to run the MCP server
CMD ["sh", "-c", "node build/index.js"]
