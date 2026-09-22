import { useEffect, useState } from 'react';
import './App.css';

const emptyForm = { title: '', author: '' };

function App() {
    const [books, setBooks] = useState();
    const [form, setForm] = useState(emptyForm);
    const [editingId, setEditingId] = useState(null);

    useEffect(() => {
        populateBooks();
    }, []);

    async function populateBooks() {
        const response = await fetch('/books');
        if (response.ok) {
            const data = await response.json();
            setBooks(data);
        }
    }

    async function handleSubmit(event) {
        event.preventDefault();

        if (editingId === null) {
            await fetch('/books', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(form)
            });
        } else {
            await fetch(`/books/${editingId}`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(form)
            });
        }

        setForm(emptyForm);
        setEditingId(null);
        await populateBooks();
    }

    function handleEdit(book) {
        setEditingId(book.id);
        setForm({ title: book.title, author: book.author });
    }

    function handleCancelEdit() {
        setEditingId(null);
        setForm(emptyForm);
    }

    async function handleDelete(id) {
        await fetch(`/books/${id}`, { method: 'DELETE' });

        if (editingId === id) {
            handleCancelEdit();
        }

        await populateBooks();
    }

    const contents = books === undefined
        ? <p><em>Loading... Please refresh once the ASP.NET backend has started.</em></p>
        : <table className="table table-striped" aria-labelledby="tableLabel">
            <thead>
                <tr>
                    <th>Title</th>
                    <th>Author</th>
                    <th></th>
                </tr>
            </thead>
            <tbody>
                {books.map(book =>
                    <tr key={book.id}>
                        <td>{book.title}</td>
                        <td>{book.author}</td>
                        <td>
                            <button type="button" onClick={() => handleEdit(book)}>Edit</button>
                            <button type="button" onClick={() => handleDelete(book.id)}>Delete</button>
                        </td>
                    </tr>
                )}
            </tbody>
        </table>;

    return (
        <div>
            <h1 id="tableLabel">Library Books</h1>
            <p>This component demonstrates fetching and managing books from the server.</p>

            <form onSubmit={handleSubmit}>
                <input
                    type="text"
                    placeholder="Title"
                    value={form.title}
                    onChange={e => setForm({ ...form, title: e.target.value })}
                    required
                />
                <input
                    type="text"
                    placeholder="Author"
                    value={form.author}
                    onChange={e => setForm({ ...form, author: e.target.value })}
                    required
                />
                <button type="submit">{editingId === null ? 'Add Book' : 'Save Changes'}</button>
                {editingId !== null && <button type="button" onClick={handleCancelEdit}>Cancel</button>}
            </form>

            {contents}
        </div>
    );
}

export default App;
