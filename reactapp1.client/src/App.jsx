import { useEffect, useRef, useState } from 'react';
import './App.css';
import { EnrollmentViz } from './EnrollmentViz';

const welcomeMessage = {
    role: 'assistant',
    text: 'Ask me about university enrollments, e.g. "How many students were in Business Administration?" or "students in Nursing last 5 years".'
};

function App() {
    const [messages, setMessages] = useState([welcomeMessage]);
    const [input, setInput] = useState('');
    const [isLoading, setIsLoading] = useState(false);
    const bottomRef = useRef(null);
    const historyRef = useRef([]);

    useEffect(() => {
        bottomRef.current?.scrollIntoView({ behavior: 'smooth' });
    }, [messages, isLoading]);

    async function handleSubmit(event) {
        event.preventDefault();

        const question = input.trim();
        if (!question || isLoading) {
            return;
        }

        setMessages(prev => [...prev, { role: 'user', text: question }]);
        setInput('');
        setIsLoading(true);

        try {
            const response = await fetch('/enrollments/query', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ question, history: historyRef.current })
            });

            if (response.ok) {
                const data = await response.json();
                setMessages(prev => [...prev, {
                    role: 'assistant',
                    text: data.answer,
                    chartType: data.chartType,
                    rows: data.rows
                }]);
                historyRef.current = [
                    ...historyRef.current,
                    { role: 'user', content: question },
                    { role: 'assistant', content: data.answer }
                ];
            } else {
                const error = await response.text();
                setMessages(prev => [...prev, {
                    role: 'assistant',
                    text: `Sorry, something went wrong (HTTP ${response.status}). ${error}`.trim()
                }]);
            }
        } catch (err) {
            setMessages(prev => [...prev, { role: 'assistant', text: `Sorry, could not reach the server: ${err.message}` }]);
        } finally {
            setIsLoading(false);
        }
    }

    function handleReset() {
        setMessages([welcomeMessage]);
        historyRef.current = [];
    }

    return (
        <div className="chat-app">
            <h1>Enrollment Assistant</h1>
            <p>
                Ask a question about enrollment data in plain English. Follow-up questions are supported.{' '}
                <button type="button" onClick={handleReset} disabled={isLoading}>New conversation</button>
            </p>

            <div className="chat-window">
                {messages.map((message, index) =>
                    <div key={index} className={`chat-row ${message.role}`}>
                        <div className={`chat-bubble ${message.role}`}>{message.text}</div>
                        {message.role === 'assistant' &&
                            <EnrollmentViz chartType={message.chartType} rows={message.rows} />}
                    </div>
                )}
                {isLoading && <div className="chat-bubble assistant loading">Thinking…</div>}
                <div ref={bottomRef} />
            </div>

            <form className="chat-input" onSubmit={handleSubmit}>
                <input
                    type="text"
                    placeholder="Ask a question…"
                    value={input}
                    onChange={e => setInput(e.target.value)}
                    disabled={isLoading}
                />
                <button type="submit" disabled={isLoading || !input.trim()}>Send</button>
            </form>
        </div>
    );
}

export default App;
