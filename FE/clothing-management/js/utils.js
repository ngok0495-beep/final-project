// ═══════════════════════════════════════════
// utils.js — Config, State, Helper Functions
// ═══════════════════════════════════════════

// ── Config ────────────────────────────────
const AUTH_URL = 'http://localhost:5001';
const USER_URL = 'http://localhost:5002';

// ── App State ─────────────────────────────
const state = {
  token: localStorage.getItem('cm_token') || null,
  user: JSON.parse(localStorage.getItem('cm_user') || 'null'),
  currentPage: 'dashboard',
  users: [],
  usersPage: 1,
  usersTotal: 0,
  userSearch: '',
  profile: null,
};

// ── HTTP helpers ──────────────────────────
function headers(extra = {}) {
  return {
    'Content-Type': 'application/json',
    ...(state.token ? { 'Authorization': `Bearer ${state.token}` } : {}),
    ...extra,
  };
}

// ── UI helpers ────────────────────────────
function toast(msg, type = '') {
  const el = document.getElementById('toast');
  el.textContent = msg;
  el.className = 'show ' + type;
  setTimeout(() => { el.className = ''; }, 3000);
}

function showAlert(id, msg, type = 'danger') {
  const el = document.getElementById(id);
  el.textContent = msg;
  el.className = `alert ${type} show`;
}

function hideAlert(id) {
  document.getElementById(id).className = 'alert danger';
}

function openModal(id)  { document.getElementById(id).classList.add('show'); }
function closeModal(id) { document.getElementById(id).classList.remove('show'); }

/**
 * Toggle loading state on a button.
 * @param {string} btnId
 * @param {boolean} loading
 * @param {string} text - HTML to show when NOT loading
 */
function setLoading(btnId, loading, text) {
  const btn = document.getElementById(btnId);
  if (!btn) return;
  btn.disabled = loading;
  btn.innerHTML = loading
    ? `<span class="spinner"></span> ${text || '...'}`
    : text;
}

// ── String helpers ────────────────────────
function initials(name) {
  if (!name) return '?';
  return name.split(' ').map(w => w[0]).join('').toUpperCase().slice(0, 2);
}

function roleLabel(r) {
  return r === 'Admin' ? 'Admin' : r === 'Staff' ? 'Nhân viên' : 'Người dùng';
}

function roleBadge(r) {
  const cls = r === 'Admin' ? 'admin' : r === 'Staff' ? 'staff' : 'user';
  return `<span class="badge ${cls}">${roleLabel(r)}</span>`;
}

function fmtDate(d) {
  if (!d) return '—';
  return new Date(d).toLocaleDateString('vi-VN');
}