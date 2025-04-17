CREATE INDEX idx_books_title ON books(title);
CREATE INDEX idx_books_author ON books(author);
CREATE INDEX idx_books_isbn ON books(isbn);
CREATE INDEX idx_books_is_borrowed ON books(is_borrowed) WHERE is_borrowed = true;
CREATE INDEX idx_books_is_deleted ON books(is_deleted) WHERE is_deleted = false;

CREATE INDEX idx_users_email ON users(email);
CREATE INDEX idx_users_name ON users(last_name, first_name);
CREATE INDEX idx_users_is_deleted ON users(is_deleted) WHERE is_deleted = false;

CREATE INDEX idx_borrowings_user_id ON borrowings(user_id);
CREATE INDEX idx_borrowings_book_id ON borrowings(book_id);
CREATE INDEX idx_borrowings_returned_status ON borrowings(returned_at) WHERE returned_at IS NULL;
CREATE INDEX idx_borrowings_dates ON borrowings(borrowed_at, returned_at);