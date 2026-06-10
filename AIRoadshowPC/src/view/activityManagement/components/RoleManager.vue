<template>
	<div>
		<div class="section-head">
			<div>
				<div class="section-title">角色配置</div>
			</div>
			<el-button type="primary" icon="el-icon-plus" size="small" @click="openCreateDialog">新增角色</el-button>
		</div>

		<el-table :data="roles" border>
			<el-table-column prop="name" label="角色名称" min-width="140" />
			<el-table-column prop="persona" label="角色定位" min-width="220" show-overflow-tooltip />
			<el-table-column prop="goal" label="核心目标" min-width="220" show-overflow-tooltip />
			<el-table-column prop="desc" label="说明" min-width="260" show-overflow-tooltip />
			<el-table-column label="操作" width="140" align="center">
				<template slot-scope="scope">
					<el-button type="text" @click="openEditDialog(scope.row)">编辑</el-button>
					<el-button type="text" class="danger-text" @click="handleDelete(scope.row)">删除</el-button>
				</template>
			</el-table-column>
		</el-table>

		<div v-if="!roles.length" class="empty-tip">暂未配置角色，请先新增至少一个角色。</div>

		<el-dialog :title="dialogTitle" :visible.sync="dialogVisible" width="560px" append-to-body>
			<el-form label-width="100px">
				<el-form-item label="角色名称" required>
					<el-input v-model.trim="form.name" maxlength="20" show-word-limit placeholder="请输入角色名称" />
				</el-form-item>
				<el-form-item label="角色定位" required>
					<el-input
						v-model.trim="form.persona"
						maxlength="50"
						show-word-limit
						placeholder="请输入角色定位"
					/>
				</el-form-item>
				<el-form-item label="核心目标" required>
					<el-input v-model.trim="form.goal" maxlength="60" show-word-limit placeholder="请输入核心目标" />
				</el-form-item>
				<el-form-item label="角色说明">
					<el-input
						v-model.trim="form.desc"
						type="textarea"
						:rows="4"
						maxlength="200"
						show-word-limit
						placeholder="请输入角色说明"
					/>
				</el-form-item>
			</el-form>
			<span slot="footer">
				<el-button @click="dialogVisible = false">取消</el-button>
				<el-button type="primary" :loading="saving" @click="handleSave">保存</el-button>
			</span>
		</el-dialog>
	</div>
</template>

<script>
const createRoleForm = () => ({
	id: '',
	name: '',
	persona: '',
	goal: '',
	desc: '',
})

export default {
	name: 'RoleManager',
	props: {
		roles: {
			type: Array,
			default: function () {
				return []
			},
		},
	},
	data() {
		return {
			dialogVisible: false,
			dialogMode: 'create',
			form: createRoleForm(),
			saving: false,
		}
	},
	computed: {
		dialogTitle() {
			return this.dialogMode === 'edit' ? '编辑角色' : '新增角色'
		},
	},
	methods: {
		openCreateDialog() {
			this.dialogMode = 'create'
			this.form = createRoleForm()
			this.dialogVisible = true
		},
		openEditDialog(row) {
			this.dialogMode = 'edit'
			this.form = Object.assign({}, row)
			this.dialogVisible = true
		},
		handleSave() {
			if (!this.form.name || !this.form.persona || !this.form.goal) {
				this.$message.warning('请完整填写角色名称、角色定位和核心目标')
				return
			}
			this.saving = true
			this.$emit('save-role', {
				mode: this.dialogMode,
				form: Object.assign({}, this.form),
				onSuccess: (savedRole) => {
					const nextRoles = this.roles.slice()
					if (this.dialogMode === 'edit') {
						const index = nextRoles.findIndex((item) => item.id === this.form.id)
						if (index !== -1) {
							nextRoles.splice(index, 1, Object.assign({}, savedRole))
						}
					} else {
						nextRoles.push(Object.assign({}, savedRole))
					}
					this.$emit('update:roles', nextRoles)
					this.dialogVisible = false
					this.saving = false
				},
				onError: () => {
					this.saving = false
				},
			})
		},
		handleDelete(row) {
			this.$confirm(`确认删除角色“${row.name}”吗？`, '提示', {
				type: 'warning',
			})
				.then(() => {
					this.$emit('delete-role', {
						row,
						onSuccess: () => {
							const nextRoles = this.roles.filter((item) => item.id !== row.id)
							this.$emit('update:roles', nextRoles)
						},
					})
				})
				.catch(() => {})
		},
	},
}
</script>

<style scoped>
.section-head {
	display: flex;
	align-items: center;
	justify-content: space-between;
	gap: 16px;
	margin-bottom: 18px;
}

.section-title {
	font-size: 18px;
	font-weight: 700;
	color: #1f2d3d;
}

.danger-text {
	color: #f56c6c;
}

.empty-tip {
	padding: 16px 0 4px;
	font-size: 13px;
	color: #909399;
}
</style>
