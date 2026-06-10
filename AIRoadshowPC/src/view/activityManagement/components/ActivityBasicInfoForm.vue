<template>
	<div>
		<div class="section-title">基础资料</div>
		<el-form label-width="130px">
			<el-form-item label="活动背景资料" required>
				<el-upload
					class="upload-panel"
					drag
					:action="uploadAction"
					multiple
					:auto-upload="true"
					:file-list="backgroundUploadList"
					:http-request="handleBackgroundUploadRequest"
					:on-remove="handleBackgroundFileRemove"
					:before-upload="beforeUpload"
					:disabled="uploading"
					accept=".pdf,.doc,.docx,.ppt,.pptx,.xls,.xlsx"
				>
					<i class="el-icon-upload upload-icon" />
					<div class="el-upload__text">
						将活动背景资料拖到此处，或<em>点击上传</em>
					</div>
					<div slot="tip" class="el-upload__tip">
						支持 PDF / Word / PPT / Excel，单个文件不超过 10MB。
					</div>
				</el-upload>
			</el-form-item>

			<el-form-item label="活动题库资料" required>
				<el-upload
					class="upload-panel"
					drag
					:action="uploadAction"
					multiple
					:auto-upload="true"
					:file-list="questionUploadList"
					:http-request="handleQuestionUploadRequest"
					:on-remove="handleQuestionFileRemove"
					:before-upload="beforeUpload"
					:disabled="uploading"
					accept=".pdf,.doc,.docx,.ppt,.pptx,.xls,.xlsx"
				>
					<i class="el-icon-upload upload-icon" />
					<div class="el-upload__text">
						将活动题库资料拖到此处，或<em>点击上传</em>
					</div>
					<div slot="tip" class="el-upload__tip">
						支持 PDF / Word / PPT / Excel，单个文件不超过 10MB。
					</div>
				</el-upload>
			</el-form-item>
		</el-form>
	</div>
</template>

<script>
const MATERIAL_TYPE_BACKGROUND = 'background'
const MATERIAL_TYPE_QUESTION = 'question'

export default {
	name: 'ActivityBasicInfoForm',
	props: {
		form: {
			type: Object,
			required: true,
		},
		uploading: {
			type: Boolean,
			default: false,
		},
	},
	computed: {
		uploadAction() {
			return `${process.env.VUE_APP_BASEURL}/Beta.FileServiceV2/Api/UserFile?modelID=61015&type=uploadfile&Storage=aicoach`
		},
		backgroundUploadList() {
			return this.createUploadList(this.form.backgroundAttachments)
		},
		questionUploadList() {
			return this.createUploadList(this.form.questionAttachments)
		},
	},
	methods: {
		createUploadList(attachments = []) {
			return (attachments || []).map((item, index) => ({
				name: item.name,
				size: item.size,
				uid: item.uid || index + 1,
				status: 'success',
			}))
		},
		beforeUpload(file) {
			const suffix = file.name.substring(file.name.lastIndexOf('.') + 1).toLowerCase()
			const allowList = ['pdf', 'doc', 'docx', 'ppt', 'pptx', 'xls', 'xlsx']
			if (allowList.indexOf(suffix) === -1) {
				this.$message.error('仅支持 PDF、Word、PPT、Excel 文件')
				return false
			}
			if (file.size / 1024 / 1024 > 10) {
				this.$message.error('单个文件大小不能超过 10MB')
				return false
			}
			return true
		},
		handleBackgroundUploadRequest(options) {
			this.handleUploadRequest(options, MATERIAL_TYPE_BACKGROUND)
		},
		handleQuestionUploadRequest(options) {
			this.handleUploadRequest(options, MATERIAL_TYPE_QUESTION)
		},
		handleUploadRequest(options, materialType) {
			this.$emit('upload-file', {
				...options,
				materialType,
			})
		},
		handleBackgroundFileRemove(file) {
			this.handleFileRemove(file, MATERIAL_TYPE_BACKGROUND)
		},
		handleQuestionFileRemove(file) {
			this.handleFileRemove(file, MATERIAL_TYPE_QUESTION)
		},
		handleFileRemove(file, materialType) {
			const sourceList =
				materialType === MATERIAL_TYPE_BACKGROUND ? this.form.backgroundAttachments : this.form.questionAttachments
			const nextAttachments = (sourceList || []).filter((item) => String(item.uid || '') !== String(file.uid || ''))
			this.$emit('update-attachments', {
				materialType,
				attachments: nextAttachments,
			})
		},
	},
}
</script>

<style scoped>
.section-title {
	margin-bottom: 20px;
	font-size: 18px;
	font-weight: 700;
	color: #1f2d3d;
}

.upload-panel {
	width: 100%;
}

.upload-panel + .upload-panel {
	margin-top: 12px;
}

.upload-icon {
	margin-top: 14px;
	font-size: 48px;
	color: #409eff;
}
</style>
