// ═══════════════════════════════════════════
// profile.js — Profile page: view & edit
// ═══════════════════════════════════════════

// ── Render page shell ─────────────────────
async function renderProfile() {
  document.getElementById('topbar-actions').innerHTML = `
    <button class="btn" onclick="openModal('modal-change-pw')">
      <i class="ti ti-lock" aria-hidden="true"></i> Đổi mật khẩu
    </button>`;

  document.getElementById('page-content').innerHTML = `
  <div class="profile-layout">
    <div class="profile-card">
      <div class="profile-avatar" id="pf-avatar">${initials(state.user?.username)}</div>
      <div class="profile-name"  id="pf-name">${state.user?.username || '—'}</div>
      <div class="profile-email" id="pf-email">${state.user?.email || '—'}</div>
      <div style="margin-bottom:20px">${roleBadge(state.user?.role || 'User')}</div>
      <div style="border-top:0.5px solid var(--border); padding-top:16px; text-align:left">
        <div style="font-size:12px; color:var(--muted); margin-bottom:4px">Auth User ID</div>
        <div style="font-family:'DM Mono',monospace; font-size:11px; color:var(--hint); word-break:break-all">
          ${state.user?.id || '—'}
        </div>
      </div>
    </div>

    <div class="profile-form-card">
      <div id="profile-form-content">
        <div style="text-align:center; padding:32px; color:var(--muted)">
          <span class="spinner"></span> Đang tải hồ sơ...
        </div>
      </div>
    </div>
  </div>`;

  await loadProfile();
}

// ── Load profile from API ─────────────────
async function loadProfile() {
  try {
    const res = await fetch(`${USER_URL}/api/users/me`, { headers: headers() });

    if (res.status === 404) {
      state.profile = null;
      renderProfileForm(null);
      return;
    }
    if (!res.ok) throw new Error();

    state.profile = await res.json();
    renderProfileForm(state.profile);
  } catch {
    document.getElementById('profile-form-content').innerHTML = `
      <div class="empty-state">
        <i class="ti ti-alert-circle"></i>
        <p>Không kết nối được UserService (port 5002)</p>
        <button class="btn" style="margin-top:12px" onclick="loadProfile()">Thử lại</button>
      </div>`;
  }
}

// ── Render the editable form ──────────────
function renderProfileForm(profile) {
  const isNew = !profile;

  document.getElementById('profile-form-content').innerHTML = `
    <div class="section-title">Thông tin cá nhân</div>
    <div class="form-row">
      <div class="form-group-inline">
        <label>Họ và tên</label>
        <input type="text" id="pf-fullname"
               value="${profile?.fullName || ''}"
               placeholder="Nguyễn Văn A"/>
      </div>
      <div class="form-group-inline">
        <label>Số điện thoại</label>
        <input type="text" id="pf-phone"
               value="${profile?.phone || ''}"
               placeholder="0901234567"/>
      </div>
    </div>
    <div class="form-row">
      <div class="form-group-inline">
        <label>Giới tính</label>
        <select id="pf-gender">
          <option value="">— Chọn —</option>
          <option value="Male"   ${profile?.gender === 'Male'   ? 'selected' : ''}>Nam</option>
          <option value="Female" ${profile?.gender === 'Female' ? 'selected' : ''}>Nữ</option>
          <option value="Other"  ${profile?.gender === 'Other'  ? 'selected' : ''}>Khác</option>
        </select>
      </div>
      <div class="form-group-inline">
        <label>Ngày sinh</label>
        <input type="date" id="pf-dob"
               value="${profile?.dateOfBirth ? profile.dateOfBirth.split('T')[0] : ''}"/>
      </div>
    </div>
    <div class="form-group-inline">
      <label>Chức vụ</label>
      <select id="pf-jobtitle">
        <option value="Khác"                ${(profile?.jobTitle || 'Khác') === 'Khác'                ? 'selected' : ''}>Khác</option>
        <option value="Admin"               ${profile?.jobTitle === 'Admin'               ? 'selected' : ''}>Admin</option>
        <option value="Quản lý"             ${profile?.jobTitle === 'Quản lý'             ? 'selected' : ''}>Quản lý</option>
        <option value="Nhân viên bán hàng"  ${profile?.jobTitle === 'Nhân viên bán hàng'  ? 'selected' : ''}>Nhân viên bán hàng</option>
        <option value="Nhân viên kho"       ${profile?.jobTitle === 'Nhân viên kho'       ? 'selected' : ''}>Nhân viên kho</option>
        <option value="Kế toán"             ${profile?.jobTitle === 'Kế toán'             ? 'selected' : ''}>Kế toán</option>
        <option value="Kinh doanh"          ${profile?.jobTitle === 'Kinh doanh'          ? 'selected' : ''}>Kinh doanh</option>
        <option value="Kỹ thuật"            ${profile?.jobTitle === 'Kỹ thuật'            ? 'selected' : ''}>Kỹ thuật</option>
      </select>
    </div>

    <div class="section-title">Địa chỉ</div>
    <div class="form-group-inline">
      <label>Số nhà / Đường</label>
      <input type="text" id="pf-street"
             value="${profile?.address?.street || ''}"
             placeholder="123 Lê Lợi"/>
    </div>
    <div class="form-row">
      <div class="form-group-inline">
        <label>Thành phố</label>
        <input type="text" id="pf-city"
               value="${profile?.address?.city || ''}"
               placeholder="TP. Hồ Chí Minh"/>
      </div>
      <div class="form-group-inline">
        <label>Tỉnh / Vùng</label>
        <input type="text" id="pf-province"
               value="${profile?.address?.province || ''}"
               placeholder="HCM"/>
      </div>
    </div>

    <div style="display:flex; justify-content:flex-end; margin-top:20px; gap:8px">
      ${isNew ? `<span style="font-size:12px; color:var(--muted); align-self:center">
        <i class="ti ti-info-circle"></i> Chưa có hồ sơ — nhấn Lưu để tạo mới
      </span>` : ''}
      <button class="btn btn-green" id="btn-save-profile" onclick="saveProfile(${isNew})">
        <i class="ti ti-device-floppy" aria-hidden="true"></i>
        ${isNew ? 'Tạo hồ sơ' : 'Lưu thay đổi'}
      </button>
    </div>`;

  // Remove Admin job title option for non-admin users
  if (state.user?.role !== 'Admin') {
    const sel = document.getElementById('pf-jobtitle');
    if (sel) {
      const adminOpt = Array.from(sel.options).find(o => o.value === 'Admin');
      if (adminOpt) adminOpt.remove();
    }
  }

  // Update sidebar & card with full name if available
  if (profile?.fullName) {
    document.getElementById('pf-avatar').textContent = initials(profile.fullName);
    document.getElementById('pf-name').textContent   = profile.fullName;
  }
}

// ── Save / Create profile ─────────────────
async function saveProfile(isNew) {
  const body = {
    authUserId:  state.user?.id || '',
    fullName:    document.getElementById('pf-fullname').value.trim(),
    phone:       document.getElementById('pf-phone').value.trim() || null,
    gender:      document.getElementById('pf-gender').value || null,
    dateOfBirth: document.getElementById('pf-dob').value || null,
    jobTitle:    document.getElementById('pf-jobtitle').value || null,
    address: {
      street:   document.getElementById('pf-street').value.trim() || null,
      city:     document.getElementById('pf-city').value.trim() || null,
      province: document.getElementById('pf-province').value.trim() || null,
    },
  };

  if (!body.fullName) { toast('Vui lòng nhập họ và tên.', 'error'); return; }

  setLoading('btn-save-profile', true, 'Đang lưu...');

  try {
    const res = await fetch(`${USER_URL}/api/users/me`, {
      method:  isNew ? 'POST' : 'PUT',
      headers: headers(),
      body:    JSON.stringify(body),
    });

    if (res.ok) {
      state.profile = await res.json();
      toast(isNew ? 'Đã tạo hồ sơ!' : 'Đã lưu thay đổi!', 'success');
      renderProfileForm(state.profile);
    } else {
      const d = await res.json().catch(() => ({}));
      toast(d.message || 'Lưu thất bại.', 'error');
    }
  } catch {
    toast('Lỗi kết nối UserService.', 'error');
  } finally {
    setLoading(
      'btn-save-profile',
      false,
      `<i class="ti ti-device-floppy"></i> ${isNew ? 'Tạo hồ sơ' : 'Lưu thay đổi'}`
    );
  }
}