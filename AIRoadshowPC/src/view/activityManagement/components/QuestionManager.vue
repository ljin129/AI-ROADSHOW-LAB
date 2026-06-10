<template>
	<div>
		<div class="section-head">
			<div>
				<div class="section-title">题库配置</div>
			</div>
			<div class="action-group">
				<el-button
					type="danger"
					plain
					size="small"
					:disabled="!selectedRows.length || batchDeleting"
					:loading="batchDeleting"
					@click="handleBatchDelete"
				>
					批量删除
				</el-button>
				<el-button type="primary" icon="el-icon-plus" size="small" @click="openCreateDialog">新增题目</el-button>
			</div>
		</div>

		<div class="filter-bar">
			<el-input
				v-model.trim="queryForm.keyword"
				clearable
				size="small"
				placeholder="请输入关键词"
				class="filter-item keyword-input"
			/>
			<el-select
				v-model="queryForm.sceneId"
				clearable
				size="small"
				placeholder="请选择业务环节"
				class="filter-item scene-select"
			>
				<el-option v-for="item in scenes" :key="item.id" :label="item.name" :value="item.id" />
			</el-select>
		</div>

		<el-table ref="questionTable" :data="pagedQuestions" border @selection-change="handleSelectionChange">
			<el-table-column type="selection" width="48" align="center" />
			<el-table-column prop="content" label="题目内容" min-width="320" show-overflow-tooltip />
			<el-table-column prop="assessmentPoints" label="考察要点" min-width="240" show-overflow-tooltip />
			<el-table-column label="所属业务环节" min-width="180">
				<template slot-scope="scope">
					{{ getSceneName(scope.row.sceneId) }}
				</template>
			</el-table-column>
			<el-table-column label="是否必答" width="100" align="center">
				<template slot-scope="scope">
					{{ scope.row.isRequired ? '是' : '否' }}
				</template>
			</el-table-column>
			<el-table-column label="操作" width="140" align="center">
				<template slot-scope="scope">
					<el-button type="text" @click="openEditDialog(scope.row)">编辑</el-button>
					<el-button type="text" class="danger-text" @click="handleDelete(scope.row)">删除</el-button>
				</template>
			</el-table-column>
		</el-table>

		<div v-if="!filteredQuestions.length" class="empty-tip">{{ emptyTipText }}</div>

		<div v-if="filteredQuestions.length" class="pagination-wrap">
			<el-pagination
				background
				layout="total, prev, pager, next"
				:current-page.sync="currentPage"
				:page-size="pageSize"
				:total="filteredQuestions.length"
			/>
		</div>

		<el-dialog :title="dialogTitle" :visible.sync="dialogVisible" width="640px" append-to-body>
			<el-form label-width="100px">
				<el-form-item label="题目内容" required>
					<el-input
						v-model.trim="form.content"
						type="textarea"
						:rows="4"
						maxlength="300"
						show-word-limit
						placeholder="请输入题目内容"
					/>
				</el-form-item>
				<el-form-item label="所属业务环节" required>
					<el-select v-model="form.sceneId" class="full-width" placeholder="请选择所属业务环节">
						<el-option v-for="item in scenes" :key="item.id" :label="item.name" :value="item.id" />
					</el-select>
				</el-form-item>
				<el-form-item label="考察要点">
					<el-input
						v-model.trim="form.assessmentPoints"
						type="textarea"
						:rows="3"
						maxlength="200"
						show-word-limit
						placeholder="请输入考察要点"
					/>
				</el-form-item>
				<el-form-item label="是否必答">
					<el-switch v-model="form.isRequired" active-text="是" inactive-text="否" />
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
const PAGE_SIZE = 15

const createQuestionForm = () => ({
	id: '',
	content: '',
	assessmentPoints: '',
	isRequired: false,
	sceneId: '',
	stageId: '',
	questionId: '',
	roleId: '',
})

const createQueryForm = () => ({
	keyword: '',
	sceneId: '',
})

export default {
	name: 'QuestionManager',
	props: {
		questions: {
			type: Array,
			default: function () {
				return []
			},
		},
		scenes: {
			type: Array,
			default: function () {
				return []
			},
		},
		dimensions: {
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
			form: createQuestionForm(),
			queryForm: createQueryForm(),
			currentPage: 1,
			pageSize: PAGE_SIZE,
			saving: false,
			selectedRows: [],
			batchDeleting: false,
		}
	},
	computed: {
		dialogTitle() {
			return this.dialogMode === 'edit' ? '编辑题目' : '新增题目'
		},
		filteredQuestions() {
			const keyword = String(this.queryForm.keyword || '').trim().toLowerCase()
			const sceneId = this.queryForm.sceneId

			return this.questions.filter((item) => {
				const matchKeyword =
					!keyword ||
					String(item.content || '')
						.toLowerCase()
						.indexOf(keyword) !== -1 ||
					String(item.assessmentPoints || '')
						.toLowerCase()
						.indexOf(keyword) !== -1
				const matchScene = !sceneId || String(item.sceneId || item.stageId || '') === String(sceneId)
				return matchKeyword && matchScene
			})
		},
		pagedQuestions() {
			const startIndex = (this.currentPage - 1) * this.pageSize
			return this.filteredQuestions.slice(startIndex, startIndex + this.pageSize)
		},
		totalPages() {
			return Math.max(1, Math.ceil(this.filteredQuestions.length / this.pageSize))
		},
		emptyTipText() {
			if (this.questions.length) {
				return '暂无符合条件的题目，请调整关键词或业务环节筛选条件。'
			}
			return '暂未配置题目，请至少新增一道题目。'
		},
	},
	watch: {
		'queryForm.keyword'() {
			this.currentPage = 1
			this.clearSelection()
		},
		'queryForm.sceneId'() {
			this.currentPage = 1
			this.clearSelection()
		},
		currentPage() {
			this.clearSelection()
		},
		totalPages(value) {
			if (this.currentPage > value) {
				this.currentPage = value
			}
		},
		scenes: {
			handler(nextScenes) {
				const exists = nextScenes.some((item) => String(item.id) === String(this.queryForm.sceneId))
				if (this.queryForm.sceneId && !exists) {
					this.queryForm.sceneId = ''
				}
			},
			deep: true,
		},
	},
	methods: {
		getSceneName(sceneId) {
			const match = this.scenes.find((item) => String(item.id) === String(sceneId))
			return match ? match.name : '未匹配业务环节'
		},
		handleSelectionChange(rows) {
			this.selectedRows = rows.slice()
		},
		clearSelection() {
			this.selectedRows = []
			this.$nextTick(() => {
				if (this.$refs.questionTable) {
					this.$refs.questionTable.clearSelection()
				}
			})
		},
		openCreateDialog() {
			if (!this.scenes.length) {
				this.$message.warning('请先完成业务环节配置，再新增题目')
				return
			}
			this.dialogMode = 'create'
			this.form = createQuestionForm()
			this.dialogVisible = true
		},
		openEditDialog(row) {
			this.dialogMode = 'edit'
			this.form = Object.assign({}, row)
			this.dialogVisible = true
		},
		handleSave() {
			if (!this.form.content || !this.form.sceneId) {
				this.$message.warning('请完整填写题目内容和所属业务环节')
				return
			}
			this.saving = true
			this.$emit('save-question', {
				mode: this.dialogMode,
				form: Object.assign({}, this.form, {
					stageId: this.form.sceneId,
				}),
				onSuccess: (savedQuestion) => {
					const nextQuestions = this.questions.slice()
					if (this.dialogMode === 'edit') {
						const index = nextQuestions.findIndex((item) => item.id === this.form.id)
						if (index !== -1) {
							nextQuestions.splice(index, 1, Object.assign({}, savedQuestion))
						}
					} else {
						nextQuestions.push(Object.assign({}, savedQuestion))
					}
					this.$emit('update:questions', nextQuestions)
					this.dialogVisible = false
					this.saving = false
					this.currentPage = this.totalPages
				},
				onError: () => {
					this.saving = false
				},
			})
		},
		handleDelete(row) {
			this.$confirm('确认删除这道题目吗？', '提示', {
				type: 'warning',
			})
				.then(() => {
					this.$emit('delete-question', {
						row,
						onSuccess: () => {
							const nextQuestions = this.questions.filter((item) => item.id !== row.id)
							this.$emit('update:questions', nextQuestions)
							this.clearSelection()
						},
					})
				})
				.catch(() => {})
		},
		handleBatchDelete() {
			if (!this.selectedRows.length || this.batchDeleting) {
				return
			}
			this.$confirm(`确认批量删除已选中的 ${this.selectedRows.length} 道题目吗？`, '提示', {
				type: 'warning',
			})
				.then(() => {
					this.batchDeleting = true
					this.$emit('delete-questions', {
						rows: this.selectedRows.slice(),
						onSuccess: (questionIds = []) => {
							const questionIdSet = new Set(questionIds.map((item) => String(item)))
							const nextQuestions = this.questions.filter(
								(item) => !questionIdSet.has(String(item.questionId || item.id))
							)
							this.$emit('update:questions', nextQuestions)
							this.batchDeleting = false
							this.clearSelection()
						},
						onError: () => {
							this.batchDeleting = false
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

.action-group {
	display: flex;
	align-items: center;
	gap: 10px;
}

.filter-bar {
	display: flex;
	flex-wrap: wrap;
	gap: 12px;
	margin-bottom: 16px;
}

.filter-item {
	width: 240px;
}

.keyword-input {
	max-width: 320px;
}

.scene-select {
	max-width: 240px;
}

.full-width {
	width: 100%;
}

.danger-text {
	color: #f56c6c;
}

.empty-tip {
	padding: 16px 0 4px;
	font-size: 13px;
	color: #909399;
}

.pagination-wrap {
	display: flex;
	justify-content: flex-end;
	margin-top: 16px;
}

@media (max-width: 768px) {
	.section-head {
		flex-direction: column;
		align-items: stretch;
	}

	.action-group {
		justify-content: flex-end;
	}

	.filter-item,
	.keyword-input,
	.scene-select {
		width: 100%;
		max-width: none;
	}
}
</style>
