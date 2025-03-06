CREATE EXTENSION IF NOT EXISTS vector;

CREATE TABLE chat_history (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_query TEXT NOT NULL,
    assistant_response TEXT NOT NULL,
    embedding VECTOR(1536),  -- Adjust based on model
    timestamp TIMESTAMPTZ DEFAULT now()
);

-- Index for efficient nearest-neighbor search
CREATE INDEX ON chat_history USING ivfflat (embedding vector_l2_ops);
