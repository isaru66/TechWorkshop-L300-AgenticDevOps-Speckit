package com.workshop.banking.mcp;

import com.workshop.banking.service.BankingException;
import io.modelcontextprotocol.spec.McpSchema.CallToolResult;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

/** Converts service failures into MCP tool error results the assistant can relay. */
final class McpErrorMapper {

    private static final Logger log = LoggerFactory.getLogger(McpErrorMapper.class);

    private McpErrorMapper() {
    }

    /** Text content for every client; structured content (objects only) lets REST Client chain ids. */
    static CallToolResult success(String json, Object value) {
        CallToolResult.Builder builder = CallToolResult.builder().addTextContent(json).isError(false);
        if (!(value instanceof java.util.Collection)) {
            builder.structuredContent(value);
        }
        return builder.build();
    }

    static CallToolResult failure(Throwable e) {
        String text;
        if (e instanceof BankingException be) {
            text = be.getCode() + ": " + be.getMessage();
        } else {
            log.error("Unexpected MCP tool failure", e);
            text = "INTERNAL_ERROR: The request could not be completed";
        }
        return CallToolResult.builder().addTextContent(text).isError(true).build();
    }
}
