// ═══════════════════════════════════════════
// users.js — Users page (Admin only)
//            List, Search, Add, Edit, Delete
// ═══════════════════════════════════════════

// ── Render page shell ─────────────────────
async function renderUsers() {
  if (state.user?.role !== 'Admin') {
    document.getElementById('page-content').innerHTML = `
      <div class="empty-state">
        <i class="ti ti-lock"></i>
        <p>Bạn không có quyền truy cập trang này.</p>
      </div>`;
    return;
  }

  document.getElementById('topbar-actions').innerHTML = '';

  document.getElementById('page-content').innerHTML = `
  <div class="toolbar">
    <div class="search-box">
      <i class="ti ti-search" aria-hidden="true"></i>
      <input type="text" id="user-search"
             placeholder="Tìm theo tên, số điện thoại..."
             oninput="onSearchUsers(this.value)"/>
    </div>
  </div>
  <div class="table-wrap">
    <table>
      <thead>
        <tr>
          <th>Người dùng</th>
          <th>Vai trò</th>
          <th>Phòng ban</th>
          <th>Điện thoại</th>
          <th>Ngày tạo</th>
          <th></th>
        </tr>
      </thead>
      <tbody id="user-table-body">
        <tr><td colspan="6" style="text-align:center; padding:32px; color:var(--muted)">
          <span class="spinner"></span> Đang tải...
        </td></tr>
      </tbody>
    </table>
    <div class="pagination" id="user-pagination" style="display:none"></div>
  </div>`;

  await loadUsers();
}

// ── Search debounce ───────────────────────
let _searchTimer;
function onSearchUsers(val) {
  clearTimeout(_searchTimer);
  state.userSearch = val;
  state.usersPage  = 1;
  _searchTimer = setTimeout(loadUsers, 350);
}

// ── Load users from API ───────────────────
async function loadUsers() {
  try {
    const params = new URLSearchParams({
      page:     state.usersPage,
      pageSize: 8,
      ...(state.userSearch ? { search: state.userSearch } : {}),
    });

    const res = await fetch(`${USER_URL}/api/users?${params}`, { headers: headers() });

    if (res.status === 403) {
      document.getElementById('user-table-body').innerHTML = `
        <tr><td colspan="6" style="text-align:center; color:var(--danger); padding:24px">
          Không có quyền truy cập.
        </td></tr>`;
      return;
    }
    if (!res.ok) throw new Error('Lỗi server');

    const data = await res.json();
    state.users      = data.items || [];
    state.usersTotal = data.totalCount || 0;
    renderUserTable(data);
  } catch {
    document.getElementById('user-table-body').innerHTML = `
      <tr><td colspan="6" style="text-align:center; color:var(--danger); padding:24px">
        <i class="ti ti-alert-circle"></i> Không kết nối được UserService (port 5002)
      </td></tr>`;
  }
}

// ── Render table rows ─────────────────────
function renderUserTable(data) {
  const tbody = document.getElementById('user-table-body');

  if (!data.items?.length) {
    tbody.innerHTML = `<tr><td colspan="6">
      <div class="empty-state">
        <i class="ti ti-users-off"></i>
        <p>Không tìm thấy người dùng nào.</p>
      </div>
    </td></tr>`;
    document.getElementById('user-pagination').style.display = 'none';
    return;
  }

  // Exclude current logged-in user from the list
  const rows = data.items.filter(u => u.authUserId !== state.user?.id);

  tbody.innerHTML = rows.map(u => `
    <tr>
      <td>
        <div style="display:flex; align-items:center; gap:10px">
          <div class="avatar" style="width:34px;height:34px;font-size:12px">
            ${initials(u.fullName)}
          </div>
          <div>
            <div style="font-weight:500">${u.fullName || '—'}</div>
            <div style="font-size:12px; color:var(--muted)">${u.authUserId?.slice(-8) || ''}</div>
          </div>
        </div>
      </td>
      <td>—</td>
      <td>${u.department || '<span style="color:var(--hint)">—</span>'}</td>
      <td>${u.phone || '<span style="color:var(--hint)">—</span>'}</td>
      <td>${fmtDate(u.createdAt)}</td>
      <td style="white-space:nowrap">
        <button class="btn btn-sm" onclick="openEditUser('${u.authUserId}')" title="Sửa">
          <i class="ti ti-pencil" aria-hidden="true"></i>
        </button>
        <button class="btn btn-sm btn-danger" onclick="deleteUser('${u.authUserId}')" title="Xóa">
          <i class="ti ti-trash" aria-hidden="true"></i>
        </button>
      </td>
    </tr>`).join('');

  _renderPagination(data);
}

// ── Pagination ────────────────────────────
function _renderPagination(data) {
  const pg = document.getElementById('user-pagination');
  if (data.totalPages <= 1) { pg.style.display = 'none'; return; }

  const pages = Array.from({ length: data.totalPages }, (_, i) => i + 1);
  pg.style.display = 'flex';
  pg.innerHTML = `
    <span>${data.totalCount} người dùng</span>
    <div class="page-btns">
      <div class="page-btn" onclick="changePage(${state.usersPage - 1})">
        <i class="ti ti-chevron-left" aria-hidden="true"></i>
      </div>
      ${pages.map(p => `
        <div class="page-btn ${p === state.usersPage ? 'active' : ''}" onclick="changePage(${p})">${p}</div>
      `).join('')}
      <div class="page-btn" onclick="changePage(${state.usersPage + 1})">
        <i class="ti ti-chevron-right" aria-hidden="true"></i>
      </div>
    </div>`;
}

function changePage(p) {
  const max = Math.ceil(state.usersTotal / 8);
  if (p < 1 || p > max) return;
  state.usersPage = p;
  loadUsers();
}

// ── Edit user ─────────────────────────────
function openEditUser(authUserId) {
  const u = state.users.find(x => x.authUserId === authUserId);
  if (!u) return;

  document.getElementById('edit-auth-user-id').value = authUserId;
  document.getElementById('edit-username').value     = u.username  || '';
  document.getElementById('edit-email').value        = u.email     || '';
  document.getElementById('edit-role').value         = u.role      || 'User';
  document.getElementById('edit-fullname').value     = u.fullName  || '';
  document.getElementById('edit-phone').value        = u.phone     || '';
  hideAlert('edit-modal-alert');
  openModal('modal-edit-user');
}

async function submitEditUser() {
  const authUserId = document.getElementById('edit-auth-user-id').value;
  const username   = document.getElementById('edit-username').value.trim();
  const email      = document.getElementById('edit-email').value.trim();
  const role       = document.getElementById('edit-role').value;
  const fullName   = document.getElementById('edit-fullname').value.trim();
  const phone      = document.getElementById('edit-phone').value.trim();

  if (!username || !email || !fullName) {
    showAlert('edit-modal-alert', 'Vui lòng điền đầy đủ tên đăng nhập, email và họ tên.');
    return;
  }

  setLoading('btn-save-edit-user', true, 'Đang lưu...');
  hideAlert('edit-modal-alert');

  try {
    // 1) Update auth account
    const resAuth = await fetch(`${AUTH_URL}/api/auth/admin/${authUserId}`, {
      method: 'PUT',
      headers: headers(),
      body: JSON.stringify({ username, email, role }),
    });
    if (!resAuth.ok) {
      const d = await resAuth.json().catch(() => ({}));
      showAlert('edit-modal-alert', d.message || 'Cập nhật tài khoản thất bại.');
      return;
    }

    // 2) Update user profile
    const resProfile = await fetch(`${USER_URL}/api/users/${authUserId}`, {
      method: 'PUT',
      headers: headers(),
      body: JSON.stringify({ fullName, email, phone: phone || null }),
    });
    if (!resProfile.ok) {
      const d = await resProfile.json().catch(() => ({}));
      showAlert('edit-modal-alert', d.message || 'Cập nhật hồ sơ thất bại.');
      return;
    }

    closeModal('modal-edit-user');
    toast('Đã cập nhật người dùng.', 'success');
    loadUsers();
  } catch {
    showAlert('edit-modal-alert', 'Lỗi kết nối server.');
  } finally {
    setLoading('btn-save-edit-user', false, '<i class="ti ti-device-floppy"></i> Lưu thay đổi');
  }
}

// ── Delete user ───────────────────────────
async function deleteUser(authUserId) {
  if (!confirm('Xác nhận xóa profile người dùng này?')) return;
  try {
    const res = await fetch(`${USER_URL}/api/users/${authUserId}`, {
      method: 'DELETE',
      headers: headers(),
    });
    if (res.ok) {
      toast('Đã xóa người dùng.', 'success');
      loadUsers();
    } else {
      toast('Xóa thất bại.', 'error');
    }
  } catch {
    toast('Lỗi kết nối.', 'error');
  }
}

// ── Add user (modal) ──────────────────────
async function submitAddUser() {
  const username = document.getElementById('modal-username').value.trim();
  const email    = document.getElementById('modal-email').value.trim();
  const password = document.getElementById('modal-password').value;

  if (!username || !email || !password) {
    showAlert('modal-alert', 'Vui lòng điền đầy đủ.');
    return;
  }

  setLoading('btn-add-user', true, 'Đang tạo...');

  try {
    // 1) Register via AuthService
    const res1 = await fetch(`${AUTH_URL}/api/auth/register`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ username, email, password, role: 'User' }),
    });
    const d1 = await res1.json();
    if (!res1.ok || !d1.success) {
      showAlert('modal-alert', d1.message || 'Đăng ký thất bại.');
      return;
    }

    // 2) Trigger profile creation on UserService (fire-and-forget)
    fetch(`${USER_URL}/api/users/${d1.user.id}`, {
      method: 'GET',
      headers: headers(),
    }).catch(() => {});

    closeModal('modal-add-user');
    ['modal-username', 'modal-email', 'modal-password'].forEach(
      id => (document.getElementById(id).value = '')
    );
    hideAlert('modal-alert');
    toast('Tạo tài khoản thành công!', 'success');
    loadUsers();
  } catch {
    showAlert('modal-alert', 'Lỗi kết nối server.');
  } finally {
    setLoading('btn-add-user', false, '<i class="ti ti-plus"></i> Tạo tài khoản');
  }
}