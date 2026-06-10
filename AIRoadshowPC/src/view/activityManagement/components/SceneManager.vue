<template>
	<div>
		<div class="section-head">
			<div>
				<div class="section-title">业务环节配置</div>
			</div>
			<el-button type="primary" icon="el-icon-plus" size="small" @click="openCreateDialog">新增业务环节</el-button>
		</div>

		<el-table :data="scenes" border>
			<el-table-column prop="name" label="业务环节名称" min-width="160" />
			<el-table-column prop="objective" label="业务环节目标" min-width="220" show-overflow-tooltip />
			<el-table-column prop="desc" label="业务环节说明" min-width="260" show-overflow-tooltip />
			<el-table-column label="操作" width="140" align="center">
				<template slot-scope="scope">
					<el-button type="text" @click="openEditDialog(scope.row)">编辑</el-button>
					<el-button type="text" class="danger-text" @click="handleDelete(scope.row)">删除</el-button>
				</template>
			</el-table-column>
		</el-table>

		<div v-if="!scenes.length" class="empty-tip">暂未配置业务环节，请先新增至少一个业务环节。</div>

		<el-dialog :title="dialogTitle" :visible.sync="dialogVisible" width="620px" append-to-body>
			<el-form label-width="100px">
				<el-form-item label="业务环节名称" required>
					<el-input v-model.trim="form.name" maxlength="30" show-word-limit placeholder="请输入业务环节名称" />
				</el-form-item>
				<el-form-item label="业务环节目标" required>
					<el-input
						v-model.trim="form.objective"
						maxlength="60"
						show-word-limit
						placeholder="请输入业务环节目标"
					/>
				</el-form-item>
				<el-form-item label="业务环节说明">
					<el-input
						v-model.trim="form.desc"
						type="textarea"
						:rows="4"
						maxlength="200"
						show-word-limit
						placeholder="请输入业务环节说明"
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
const createSceneForm = () => ({
	id: '',
	name: '',
	objective: '',
	desc: '',
})

export default {
	name: 'SceneManager',
	props: {
		scenes: {
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
			form: createSceneForm(),
			saving: false,
		}
	},
	computed: {
		dialogTitle() {
			return this.dialogMode === 'edit' ? '编辑业务环节' : '新增业务环节'
		},
	},
	methods: {
		openCreateDialog() {
			this.dialogMode = 'create'
			this.form = createSceneForm()
			this.dialogVisible = true
		},
		openEditDialog(row) {
			this.dialogMode = 'edit'
			this.form = Object.assign({}, row)
			this.dialogVisible = true
		},
		handleSave() {
			if (!this.form.name || !this.form.objective) {
				this.$message.warning('请完整填写业务环节名称和业务环节目标')
				return
			}
			this.saving = true
			this.$emit('save-scene', {
				mode: this.dialogMode,
				form: Object.assign({}, this.form),
				onSuccess: (savedScene) => {
					const nextScenes = this.scenes.slice()
					if (this.dialogMode === 'edit') {
						const index = nextScenes.findIndex((item) => item.id === this.form.id)
						if (index !== -1) {
							nextScenes.splice(index, 1, Object.assign({}, savedScene))
						}
					} else {
						nextScenes.push(Object.assign({}, savedScene))
					}
					this.$emit('update:scenes', nextScenes)
					this.dialogVisible = false
					this.saving = false
				},
				onError: () => {
					this.saving = false
				},
			})
		},
		handleDelete(row) {
			this.$confirm(`确认删除业务环节“${row.name}”吗？`, '提示', {
				type: 'warning',
			})
				.then(() => {
					this.$emit('delete-scene', {
						row,
						onSuccess: () => {
							const nextScenes = this.scenes.filter((item) => item.id !== row.id)
							this.$emit('update:scenes', nextScenes)
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
