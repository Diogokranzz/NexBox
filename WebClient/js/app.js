const API_BASE_URL = 'http://localhost:5000/api';

const state = {
    token: localStorage.getItem('token') || null,
    username: localStorage.getItem('username') || null,
    currentPage: 1,
    pageSize: 8
};

const els = {
    authView: document.getElementById('auth-view'),
    dashView: document.getElementById('dashboard-view'),
    loginForm: document.getElementById('login-form'),
    regForm: document.getElementById('register-form'),
    toLoginBtn: document.getElementById('go-to-login'),
    toRegBtn: document.getElementById('go-to-register'),
    welcomeMsg: document.getElementById('welcome-message'),
    logoutBtn: document.getElementById('logout-btn'),
    productsBody: document.getElementById('products-body'),
    prevPageBtn: document.getElementById('prev-page'),
    nextPageBtn: document.getElementById('next-page'),
    pageInfo: document.getElementById('page-info'),
    addProductBtn: document.getElementById('add-product-btn'),
    productModal: document.getElementById('product-modal'),
    closeModalBtn: document.getElementById('close-modal-btn'),
    productForm: document.getElementById('product-form'),
    modalTitle: document.getElementById('modal-title'),
    toastContainer: document.getElementById('toast-container')
};

function init() {
    if (state.token) {
        showDashboard();
    } else {
        showAuth();
    }
}

function showAuth() {
    els.authView.classList.add('active');
    els.authView.classList.remove('hidden');
    els.dashView.classList.remove('active');
    els.dashView.classList.add('hidden');
}

function showDashboard() {
    els.authView.classList.remove('active');
    els.authView.classList.add('hidden');
    els.dashView.classList.add('active');
    els.dashView.classList.remove('hidden');
    els.welcomeMsg.textContent = `Hello, ${state.username}`;
    loadProducts();
}

function showToast(message, type = 'info') {
    const toast = document.createElement('div');
    toast.className = `toast ${type}`;
    toast.textContent = message;
    els.toastContainer.appendChild(toast);
    setTimeout(() => toast.remove(), 3000);
}

async function apiFetch(endpoint, options = {}) {
    const headers = { 'Content-Type': 'application/json' };
    if (state.token) {
        headers['Authorization'] = `Bearer ${state.token}`;
    }
    
    const config = { ...options, headers: { ...headers, ...options.headers } };
    
    try {
        const response = await fetch(`${API_BASE_URL}${endpoint}`, config);
        
        if (response.status === 401) {
            handleLogout();
            throw new Error("Session expired. Please login again.");
        }
        
        let text = "";
        try {
            text = await response.text();
        } catch(e) {}
        
        const data = text ? JSON.parse(text) : null;
        
        if (!response.ok) {
            throw new Error(data?.mensagem || data?.errors?.Nome?.[0] || 'An error occurred during network request');
        }
        return data;
    } catch (err) {
        console.error("API Error:", err);
        showToast(err.message, 'error');
        throw err;
    }
}

els.toRegBtn.addEventListener('click', () => {
    els.loginForm.classList.add('hidden');
    els.regForm.classList.remove('hidden');
});

els.toLoginBtn.addEventListener('click', () => {
    els.regForm.classList.add('hidden');
    els.loginForm.classList.remove('hidden');
});

els.loginForm.addEventListener('submit', async (e) => {
    e.preventDefault();
    const btn = e.target.querySelector('button');
    btn.disabled = true;
    btn.textContent = 'Authenticating...';

    const data = {
        username: document.getElementById('login-username').value,
        password: document.getElementById('login-password').value
    };
    
    try {
        const res = await apiFetch('/auth/login', {
            method: 'POST',
            body: JSON.stringify(data)
        });
        
        localStorage.setItem('token', res.accessToken);
        localStorage.setItem('username', data.username);
        state.token = res.accessToken;
        state.username = data.username;
        
        showToast('Login successful!', 'success');
        showDashboard();
    } catch (err) {} finally {
        btn.disabled = false;
        btn.textContent = 'Login';
    }
});

els.regForm.addEventListener('submit', async (e) => {
    e.preventDefault();
    const btn = e.target.querySelector('button');
    btn.disabled = true;
    btn.textContent = 'Creating Account...';

    const data = {
        username: document.getElementById('reg-username').value,
        email: document.getElementById('reg-email').value,
        password: document.getElementById('reg-password').value
    };
    
    try {
        const res = await apiFetch('/auth/register', {
            method: 'POST',
            body: JSON.stringify(data)
        });
        
        localStorage.setItem('token', res.accessToken);
        localStorage.setItem('username', data.username);
        state.token = res.accessToken;
        state.username = data.username;
        
        showToast('Account created successfully!', 'success');
        showDashboard();
    } catch (err) {} finally {
        btn.disabled = false;
        btn.textContent = 'Register';
    }
});

function handleLogout() {
    localStorage.removeItem('token');
    localStorage.removeItem('username');
    state.token = null;
    state.username = null;
    showAuth();
    els.loginForm.reset();
    els.regForm.reset();
}

els.logoutBtn.addEventListener('click', handleLogout);

async function loadProducts(page = state.currentPage) {
    els.productsBody.innerHTML = `<tr><td colspan="5" style="text-align:center">Loading products...</td></tr>`;
    try {
        const res = await apiFetch(`/products?pageNumber=${page}&pageSize=${state.pageSize}`);
        renderProducts(res.data);
        updatePagination(res);
        state.currentPage = page;
    } catch (err) {
        els.productsBody.innerHTML = `<tr><td colspan="5" style="text-align:center; color: #ef4444;">Failed to load data.</td></tr>`;
    }
}

function renderProducts(products) {
    els.productsBody.innerHTML = '';
    if(!products || products.length === 0) {
        els.productsBody.innerHTML = `<tr><td colspan="5" style="text-align:center">No products found. Add one above!</td></tr>`;
        return;
    }

    products.forEach(p => {
        const tr = document.createElement('tr');
        tr.innerHTML = `
            <td><span style="color:var(--text-muted)">#${p.id}</span></td>
            <td><strong>${p.nome}</strong></td>
            <td><span style="color:#22c55e">$${p.preco.toFixed(2)}</span></td>
            <td>${p.estoque} units</td>
            <td class="action-btns">
                <button class="edit-btn" onclick="openModal(${p.id}, '${p.nome.replace(/'/g, "\\'")}', ${p.preco}, ${p.estoque})" title="Edit">Edit</button>
                <button class="del-btn" onclick="deleteProduct(${p.id})" title="Delete">Del</button>
            </td>
        `;
        els.productsBody.appendChild(tr);
    });
}

function updatePagination(res) {
    els.pageInfo.textContent = `Page ${res.pageNumber} of ${res.totalPages || Math.max(1, res.pageNumber)}`;
    els.prevPageBtn.disabled = !res.hasPreviousPage;
    els.nextPageBtn.disabled = !res.hasNextPage;
}

els.prevPageBtn.addEventListener('click', () => loadProducts(state.currentPage - 1));
els.nextPageBtn.addEventListener('click', () => loadProducts(state.currentPage + 1));

els.addProductBtn.addEventListener('click', () => openModal());

els.closeModalBtn.addEventListener('click', () => {
    els.productModal.classList.remove('active');
});

window.openModal = (id = '', nome = '', preco = '', estoque = '') => {
    document.getElementById('prod-id').value = id;
    document.getElementById('prod-nome').value = nome;
    document.getElementById('prod-preco').value = preco;
    document.getElementById('prod-estoque').value = estoque;
    
    els.modalTitle.textContent = id ? 'Edit Product' : 'Add New Product';
    els.productModal.classList.add('active');
    setTimeout(() => document.getElementById('prod-nome').focus(), 100);
};

els.productForm.addEventListener('submit', async (e) => {
    e.preventDefault();
    const btn = e.target.querySelector('.primary-btn');
    btn.disabled = true;

    const id = document.getElementById('prod-id').value;
    const isEdit = !!id;
    
    const payload = {
        nome: document.getElementById('prod-nome').value,
        preco: parseFloat(document.getElementById('prod-preco').value),
        estoque: parseInt(document.getElementById('prod-estoque').value, 10)
    };
    
    try {
        if (isEdit) {
            await apiFetch(`/products/${id}`, { method: 'PUT', body: JSON.stringify(payload) });
            showToast('Product updated successfully!', 'success');
        } else {
            await apiFetch(`/products`, { method: 'POST', body: JSON.stringify(payload) });
            showToast('Product created successfully!', 'success');
        }
        els.productModal.classList.remove('active');
        loadProducts();
    } catch (err) {} finally {
        btn.disabled = false;
    }
});

window.deleteProduct = async (id) => {
    if(!confirm("Are you sure you want to delete this product?")) return;
    try {
        await apiFetch(`/products/${id}`, { method: 'DELETE' });
        showToast('Product deleted successfully!', 'success');
        

        loadProducts();
    } catch (err) {}
};


init();
